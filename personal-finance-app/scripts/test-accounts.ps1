# Execute a partir da raiz: .\scripts\test-accounts.ps1
# Requer Docker/PostgreSQL, migrations, seed, API em Development na porta 5185.
param([string]$BaseUrl = "http://localhost:5185")

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$createdAccountId = $null
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Json($value) {
    return ConvertTo-Json -InputObject $value -Compress -Depth 8
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
            throw "Esperava HTTP $expectedCode, mas recebi HTTP $actual. $($_.Exception.Message)"
        }
    }
}

try {
    Write-Host '1. Confirmar API e base de dados...'
    $health = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health"
    if ($health.database -ne 'connected') { throw 'A base de dados nao esta ligada.' }

    Write-Host '2. Confirmar que o CRUD exige login (401)...'
    Assert-StatusCode { Invoke-RestMethod -Uri "$BaseUrl/api/accounts" } 401

    Write-Host '3. Login do demo1...'
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" `
        -Body (Json @{ email = 'demo1@example.test'; password = 'Demo123!Change' }) `
        -ContentType 'application/json' -WebSession $session
    if (-not $login.id) { throw 'O login nao devolveu o identificador do utilizador.' }

    Write-Host '4. POST: criar uma conta de teste...'
    $created = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/accounts" `
        -Body (Json @{ name = 'Conta de teste CRUD'; type = 'Savings'; initialBalance = 100.25 }) `
        -ContentType 'application/json' -WebSession $session
    $createdAccountId = $created.id
    if (-not $createdAccountId -or $created.name -ne 'Conta de teste CRUD') {
        throw 'Falha na criacao da conta.'
    }

    Write-Host '5. GET /api/accounts e GET /api/accounts/{id}...'
    $all = @(Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/accounts" -WebSession $session)
    if (-not ($all | Where-Object { $_.id -eq $createdAccountId })) {
        throw 'A conta criada nao aparece na listagem.'
    }
    $byId = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/accounts/$createdAccountId" -WebSession $session
    if ($byId.initialBalance -ne 100.25) { throw 'O saldo inicial nao foi guardado corretamente.' }

    Write-Host '6. PUT: alterar nome, tipo e saldo...'
    $updated = Invoke-RestMethod -Method Put -Uri "$BaseUrl/api/accounts/$createdAccountId" `
        -Body (Json @{ name = 'Conta atualizada'; type = 'Checking'; initialBalance = 250.75 }) `
        -ContentType 'application/json' -WebSession $session
    if ($updated.type -ne 'Checking' -or $updated.initialBalance -ne 250.75) {
        throw 'Falha na atualizacao da conta.'
    }

    Write-Host '7. Validacao: tipo invalido recebe HTTP 400...'
    Assert-StatusCode {
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/accounts" `
            -Body (Json @{ name = 'Invalida'; type = 'CreditCard'; initialBalance = 0 }) `
            -ContentType 'application/json' -WebSession $session
    } 400

    Write-Host '8. Isolamento: demo2 nao consegue consultar a conta de demo1 (404)...'
    $sessionOther = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" `
        -Body (Json @{ email = 'demo2@example.test'; password = 'Demo123!Change' }) `
        -ContentType 'application/json' -WebSession $sessionOther | Out-Null
    Assert-StatusCode {
        Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/accounts/$createdAccountId" -WebSession $sessionOther
    } 404

    Write-Host '9. DELETE: eliminar conta sem transacoes...'
    Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/accounts/$createdAccountId" -WebSession $session | Out-Null
    $createdAccountId = $null
    Assert-StatusCode {
        Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/accounts/$($created.id)" -WebSession $session
    } 404

    Write-Host '10. Regra de negocio: nao eliminar conta com transacoes (409)...'
    # Nao usar @(Invoke-RestMethod ...) | Where-Object: para respostas JSON que
    # sao arrays, o PowerShell pode enviar o array inteiro como um so objeto.
    # Selecionamos uma conta atraves de uma transacao efetivamente existente.
    $transactions = Invoke-RestMethod -Method Get `
        -Uri "$BaseUrl/dev/users/$($login.id)/transactions"
    $accountIdWithTransactions = $null
    foreach ($transaction in $transactions) {
        if ($transaction.accountId) {
            $accountIdWithTransactions = [string]$transaction.accountId
            break
        }
    }
    if (-not $accountIdWithTransactions) {
        throw 'O demo1 nao tem transacoes de demonstracao. O seeder so popula dados quando o utilizador ainda nao tem contas; confirma os dados existentes antes de testar o 409.'
    }

    # Confirma que esta conta pertence ao demo1 e tem um GUID valido.
    $accounts = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/accounts" -WebSession $session
    $accountFound = $false
    foreach ($account in $accounts) {
        if ([string]$account.id -eq $accountIdWithTransactions) {
            $accountFound = $true
            break
        }
    }
    if (-not $accountFound) {
        throw "A transacao refere a conta $accountIdWithTransactions, mas esta conta nao aparece na listagem do demo1."
    }
    $parsedId = [Guid]::Empty
    if (-not [Guid]::TryParse($accountIdWithTransactions, [ref]$parsedId)) {
        throw "O ID da conta selecionada nao e um GUID valido: $accountIdWithTransactions"
    }

    Assert-StatusCode {
        Invoke-RestMethod -Method Delete `
            -Uri "$BaseUrl/api/accounts/$accountIdWithTransactions" -WebSession $session
    } 409

    Write-Host 'OK: CRUD, validacoes, autenticacao, isolamento e FK testados.' -ForegroundColor Green
}
finally {
    # Tenta nao deixar a conta de teste na base de dados em caso de falha.
    if ($createdAccountId) {
        try {
            Invoke-RestMethod -Method Delete -Uri "$BaseUrl/api/accounts/$createdAccountId" `
                -WebSession $session | Out-Null
        }
        catch {
            Write-Warning "Nao foi possivel limpar a conta de teste $createdAccountId."
        }
    }
}
