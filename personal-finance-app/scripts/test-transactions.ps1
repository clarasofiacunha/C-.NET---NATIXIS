# Na raiz do projeto: .\scripts\test-transactions.ps1
# Requer API e PostgreSQL a correr, migrations aplicadas, seed e ambiente Development.
param([string]$BaseUrl = "http://localhost:5185")

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$transactionId = $null

function Json($value) {
    ConvertTo-Json -InputObject $value -Compress -Depth 8
}

function Assert-StatusCode($script, [int]$expectedCode) {
    try {
        & $script | Out-Null
        throw "Esperava HTTP $expectedCode, mas o pedido foi aceite."
    }
    catch {
        if ($_.Exception.Message -like 'Esperava HTTP*') { throw }
        $actual = [int]$_.Exception.Response.StatusCode
        if ($actual -ne $expectedCode) {
            throw "Esperava HTTP $expectedCode, recebi HTTP $actual. $($_.Exception.Message)"
        }
    }
}

try {
    Write-Host '1. Verificar API e base de dados...'
    $health = Invoke-RestMethod -Uri "$BaseUrl/health"
    if ($health.database -ne 'connected') { throw 'PostgreSQL indisponivel.' }

    Write-Host '2. Testar autenticacao obrigatoria (401)...'
    Assert-StatusCode { Invoke-RestMethod -Uri "$BaseUrl/api/transactions" } 401

    Write-Host '3. Entrar como demo1 pelo endpoint de autenticacao existente...'
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" `
        -Body (Json @{ email = 'demo1@example.test'; password = 'Demo123!Change' }) `
        -ContentType 'application/json' -WebSession $session
    if (-not $login.id) { throw 'O login nao devolveu o ID do utilizador.' }

    Write-Host '4. Obter conta e categoria de despesa do seed...'
    $accounts = Invoke-RestMethod -Uri "$BaseUrl/api/accounts" -WebSession $session
    $categories = Invoke-RestMethod -Uri "$BaseUrl/dev/users/$($login.id)/categories"
    $account = $accounts | Select-Object -First 1
    $expense = $categories | Where-Object { $_.type -eq 'Expense' -or $_.type -eq 1 } | Select-Object -First 1
    $income = $categories | Where-Object { $_.type -eq 'Income' -or $_.type -eq 0 } | Select-Object -First 1
    if (-not $account -or -not $expense -or -not $income) {
        throw 'Dados de demonstracao insuficientes: confirma se executaste o --seed.'
    }

    $request = @{
        accountId = $account.id
        categoryId = $expense.id
        amount = 12.75
        type = 'Expense'
        date = '2026-11-03T12:30:00Z'
        description = '  Teste CRUD de transactions  '
    }

    Write-Host '5. POST: criar uma despesa...'
    $created = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/transactions" `
        -Body (Json $request) -ContentType 'application/json' -WebSession $session
    $transactionId = $created.id
    if (-not $transactionId -or $created.description -ne 'Teste CRUD de transactions') {
        throw 'A transacao nao foi criada corretamente.'
    }

    Write-Host '6. GET por ID e GET com filtro de data e tipo...'
    $byId = Invoke-RestMethod -Uri "$BaseUrl/api/transactions/$transactionId" -WebSession $session
    if ($byId.amount -ne 12.75) { throw 'O valor lido e diferente do valor criado.' }
    $filtered = Invoke-RestMethod -Uri "$BaseUrl/api/transactions?from=2026-11-03&to=2026-11-03&type=Expense" -WebSession $session
    if (-not ($filtered | Where-Object { $_.id -eq $transactionId })) {
        throw 'A transacao nao aparece no filtro por data/tipo.'
    }

    Write-Host '7. PUT: editar o montante...'
    $request.amount = 34.50
    $request.description = 'Edicao de teste'
    $updated = Invoke-RestMethod -Method Put -Uri "$BaseUrl/api/transactions/$transactionId" `
        -Body (Json $request) -ContentType 'application/json' -WebSession $session
    if ($updated.amount -ne 34.50 -or $updated.description -ne 'Edicao de teste') {
        throw 'A transacao nao foi atualizada corretamente.'
    }

    Write-Host '8. Validacao: montante negativo e tipo/categoria incoerentes (400)...'
    $request.amount = -5
    Assert-StatusCode {
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/transactions" `
            -Body (Json $request) -ContentType 'application/json' -WebSession $session
    } 400
    $request.amount = 10
    $request.categoryId = $income.id
    Assert-StatusCode {
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/transactions" `
            -Body (Json $request) -ContentType 'application/json' -WebSession $session
    } 400

    Write-Host '9. Outro utilizador nao pode consultar ou apagar a transacao (404)...'
    $otherSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" `
        -Body (Json @{ email = 'demo2@example.test'; password = 'Demo123!Change' }) `
        -ContentType 'application/json' -WebSession $otherSession | Out-Null
    Assert-StatusCode {
        Invoke-RestMethod -Uri "$BaseUrl/api/transactions/$transactionId" -WebSession $otherSession
    } 404
    Assert-StatusCode {
        Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/transactions/$transactionId" -WebSession $otherSession
    } 404

    Write-Host '10. DELETE: apagar e confirmar 404...'
    Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/transactions/$transactionId" `
        -WebSession $session | Out-Null
    $deletedId = $transactionId
    $transactionId = $null
    Assert-StatusCode {
        Invoke-RestMethod -Uri "$BaseUrl/api/transactions/$deletedId" -WebSession $session
    } 404

    Write-Host 'OK: CRUD de transacoes, filtros, validacoes e isolamento testados.' -ForegroundColor Green
}
finally {
    # Limpar a transacao criada mesmo que um teste intermedio falhe.
    if ($transactionId) {
        try {
            Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/transactions/$transactionId" `
                -WebSession $session | Out-Null
        }
        catch {
            Write-Warning "Limpeza manual necessaria para a transacao $transactionId."
        }
    }
}
