# CRUD de contas — implementação e testes

Esta implementação usa o modelo `Account` existente e a migration `InitialCreate`.
**Não precisas de criar outra migration.**

## 1. Corrigir o diretório no PowerShell

Os comandos `docker compose` e `dotnet restore PersonalFinance.slnx` só funcionam
no diretório que contém esses ficheiros. A localização do repositório original é:

```powershell
cd 'C:\Users\BiancaLima\Documents\Csharp\App\C-.NET---NATIXIS\personal-finance-app'
Test-Path .\docker-compose.yml  # True
Test-Path .\PersonalFinance.slnx # True
```

A pasta chama-se `personal-finance-app` no código original e deve ser selecionada
em **File > Open Folder** no VS Code.

## 2. Iniciar a base de dados

```powershell
# Na raiz do projeto:
docker compose up -d
docker compose ps
```

Se não existe `.env`, podes criar com `Copy-Item .env.example .env` e editar as
credenciais em `.env` antes de criar o contentor pela primeira vez.

> Uma alteração de `POSTGRES_USER` num `.env` posterior **não altera o utilizador**
> de um volume PostgreSQL já inicializado. Não elimines volumes para corrigir
> erros de login se pretendes preservar os dados.

## 3. Restaurar o .NET e a ferramenta EF

```powershell
dotnet --version         # SDK 10.x
dotnet restore PersonalFinance.slnx
dotnet tool restore      # agora existe .config/dotnet-tools.json, dotnet-ef 8.0.33
dotnet build PersonalFinance.slnx
```

A aplicação usa .NET 10, mas os pacotes EF Core do ZIP estão na versão **8.x**.
Por isso o manifesto fixa `dotnet-ef` em 8.0.33, a versão EF Core restaurada
no ZIP. A versão 8 do EF Core é compatível com o runtime .NET 10.

## 4. Connection string e migração

A API lê `ConnectionStrings:DefaultConnection` dos **User Secrets** do projeto.
Ajusta `Database`, `Username`, `Password` e `Port` exatamente aos valores
existentes no teu `.env` (a porta é a `POSTGRES_PORT` do host).

```powershell
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Host=localhost;Port=5432;Database=personal_finance;Username=UTILIZADOR_DO_ENV;Password=PASSWORD_DO_ENV' --project backend/PersonalFinance.Api

dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

Não voltes a executar `dotnet new tool-manifest` (o ficheiro já vem no projeto
atualizado) nem cries uma segunda migration `InitialCreate`.

## 5. Seed e arranque da API

O seed existente cria 4 utilizadores e duas contas por utilizador; precisa
que as migrations já estejam aplicadas. Executa em terminal separado,
com a API parada ou com a porta 5185 livre:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http -- --seed
```

Este comando insere os dados (idempotente para contas já existentes) e arranca
a API no `http://localhost:5185`. Não precisas de voltar a correr o seed a cada
arranque; nas execuções seguintes basta:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

Confirma:

- `http://localhost:5185/` — API online.
- `http://localhost:5185/health` — API e PostgreSQL conectados.
- `http://localhost:5185/dev/users` — utilizadores de exemplo (apenas Development).

## 6. Login temporário para testes

O módulo definitivo de autenticação ainda não foi implementado no ZIP.
Para que o CRUD já tenha proteção por utilizador, existe um login de
**desenvolvimento** `POST /dev/auth/login`, que usa ASP.NET Identity e devolve
um cookie de sessão. Os endpoints `/dev/...` devolvem 404 fora de Development.
O mecanismo definitivo de login deverá ser desenvolvido na tarefa de autenticação.

No PowerShell:

```powershell
$base = 'http://localhost:5185'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-RestMethod -Uri "$base/dev/auth/login" -Method Post `
  -ContentType 'application/json' `
  -Body '{"email":"demo1@example.test","password":"Demo123!Change"}' `
  -WebSession $session
$login
```

O cookie é guardado na variável `$session`. Inclui sempre `-WebSession $session`
nos pedidos ao CRUD. No React, usar `credentials: 'include'` em `fetch` no
ambiente local; CORS foi configurado para aceitar credenciais nas origens
locais previstas no projeto.

## 7. Operações REST

| Método | Rota | Resposta de sucesso | Comportamento |
| --- | --- | --- | --- |
| GET | `/api/accounts` | 200 | Lista apenas as contas do utilizador autenticado. |
| GET | `/api/accounts/{id}` | 200 | Devolve uma conta do utilizador. |
| POST | `/api/accounts` | 201 + `Location` | Cria uma conta para o utilizador autenticado. |
| PUT | `/api/accounts/{id}` | 200 | Atualiza nome, tipo e saldo inicial. |
| DELETE | `/api/accounts/{id}` | 204 | Elimina uma conta sem transações. |

`id` é um UUID (GUID). O JSON de criação/edição:

```json
{
  "name": "Conta Poupança",
  "type": "Savings",
  "initialBalance": 250.75
}
```

Tipos suportados: `Checking` (à ordem), `Savings` (poupança), `Cash` (numerário).
`name` é obrigatório, após remover espaços exteriores, até 120 caracteres.
`initialBalance` é obrigatório, decimal com até 2 casas decimais, e pode ser
negativo (por exemplo, conta com descoberto).

Códigos de erro: **400** (validação), **401** (sem sessão), **404** (inexistente
ou pertence a outro utilizador), **409** (tentativa de apagar uma conta com
transações). Nunca se aceita `userId` da pessoa que chama o CRUD: é obtido
exclusivamente a partir do cookie autenticado.

## 8. Exemplos no PowerShell

```powershell
# Criar
$created = Invoke-RestMethod -Method Post -Uri "$base/api/accounts" `
  -WebSession $session -ContentType 'application/json' `
  -Body '{"name":"Poupança extra","type":"Savings","initialBalance":100.00}'
$id = $created.id

# Listar
Invoke-RestMethod -Method Get -Uri "$base/api/accounts" -WebSession $session

# Consultar uma conta
Invoke-RestMethod -Method Get -Uri "$base/api/accounts/$id" -WebSession $session

# Atualizar
Invoke-RestMethod -Method Put -Uri "$base/api/accounts/$id" `
  -WebSession $session -ContentType 'application/json' `
  -Body '{"name":"Poupança atualizada","type":"Savings","initialBalance":150.50}'

# Apagar (se nao tiver transacoes)
Invoke-RestMethod -Method Delete -Uri "$base/api/accounts/$id" -WebSession $session
```

## 9. Teste automático de ponta a ponta (PowerShell)

Depois de `docker compose up -d`, `database update`, `--seed` e arranque da API:

```powershell
.\scripts\test-accounts.ps1
```

Testa login, sessão obrigatória, POST, GET (lista/id), PUT, DELETE,
validação, isolamento entre `demo1` e `demo2` e impedimento de apagar
uma conta com transações. Apaga a conta temporária ao terminar.

## 10. Arquivos envolvidos e decisões

- `Controllers/AccountsController.cs`: rotas e regras HTTP, todas com `[Authorize]`.
- `DTOs/Accounts/AccountRequestDto.cs`: valida a entrada, sem aceitar `UserId`.
- `DTOs/Accounts/AccountResponseDto.cs`: define campos que são enviados ao cliente.
- `Program.cs`: configuração do JSON dos tipos de conta, SignInManager e CORS local.
- `DevelopmentController.cs`: login/logout apenas para desenvolvimento.
- `.config/dotnet-tools.json`: garante que a equipa usa a mesma versão do EF CLI.
- `scripts/test-accounts.ps1`: teste de integração manual e repetível.

**Não foi alterada a entidade `Account` nem `AppDbContext`:** estes já contêm
a tabela, o relacionamento com o dono, a precisão decimal do saldo e as FKs.
A eliminação de uma conta com transações é proibida pelo desenho da BD
(`DeleteBehavior.Restrict`); a API traduz essa situação para HTTP 409.

**Fora de escopo desta tarefa:** cálculos de saldo atual incluindo transações,
transferências e fecho/arquivamento de contas, páginas React de gestão de
contas, login definitivo para produção e proteção CSRF da futura sessão web.
