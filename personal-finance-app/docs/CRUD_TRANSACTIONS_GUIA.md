# CRUD de transacoes — guia de implementacao e testes

## 1. Objetivo

Permitir a um utilizador autenticado registar, listar, consultar, alterar e eliminar
receitas (`Income`) e despesas (`Expense`), com filtros opcionais por intervalo de datas.

**Esta tarefa e de backend.** O modelo `FinanceTransaction`, o enum `TransactionType`,
o `DbSet<FinanceTransaction> Transactions` e a migration `InitialCreate` ja existem.
**Nao e necessario adicionar uma migration** para este CRUD.

## 2. Ficheiros criados e alterados

Criados:

- `backend/PersonalFinance.Api/Controllers/TransactionsController.cs` — os cinco endpoints REST;
- `backend/PersonalFinance.Api/DTOs/Transactions/TransactionRequestDto.cs` — campos e validacoes de entrada;
- `backend/PersonalFinance.Api/DTOs/Transactions/TransactionResponseDto.cs` — resposta publica sem `UserId`;
- `backend/PersonalFinance.Tests/TransactionsControllerTests.cs` — testes sem PostgreSQL;
- `scripts/test-transactions.ps1` — teste HTTP manual/repetivel.

Alterados:

- `backend/PersonalFinance.Api/Program.cs` — permite escrever `"Income"` e `"Expense"` em JSON;
- `backend/PersonalFinance.Tests/SwaggerEndpointsTests.cs` — verifica os endpoints no Swagger e o HTTP 401.

## 3. Operacoes HTTP

| Metodo | URL | Sucesso |
| --- | --- | --- |
| `GET` | `/api/transactions` | 200, lista do utilizador |
| `GET` | `/api/transactions/{id}` | 200, objeto do utilizador |
| `POST` | `/api/transactions` | 201 + `Location`, transacao criada |
| `PUT` | `/api/transactions/{id}` | 200, transacao substituida |
| `DELETE` | `/api/transactions/{id}` | 204, sem corpo |

Filtros de listagem opcionais:

- `from=2026-11-01` — a partir do inicio desse dia, em UTC;
- `to=2026-11-30` — ate ao final desse dia, em UTC, **inclusive**;
- `type=Income` ou `type=Expense`;
- `accountId=<guid>`;
- `categoryId=<guid>`.

Exemplo:

```text
GET /api/transactions?from=2026-11-01&to=2026-11-30&type=Expense
```

Por omissao, as transacoes vem da mais recente para a mais antiga.

### Exemplo de POST/PUT

```json
{
  "accountId": "COLOCA_AQUI_GUID_DE_UMA_CONTA_TUA",
  "categoryId": "COLOCA_AQUI_GUID_DE_UMA_CATEGORIA_DE_DESPESA_TUA",
  "amount": 24.50,
  "type": "Expense",
  "date": "2026-11-03T12:30:00Z",
  "description": "Supermercado"
}
```

**Nota:** `accountId` e `categoryId` devem ser GUIDs reais da BD. O cliente
nao fornece `userId`. Em cada escrita, o backend confirma que a conta e a
categoria pertencem ao utilizador autenticado. O tipo da transacao e independente
do tipo configurado na categoria; por exemplo, uma receita como subsidio de
alimentacao pode usar a categoria "Alimentacao". A data e gravada em UTC; o
pedido deve usar ISO 8601 com `Z` ou um offset (por exemplo `+01:00`).

### Campos da resposta

- `id`, `accountId`, `accountName`, `categoryId`, `categoryName`,
  `amount`, `type`, `date`, `description`.

## 4. Regras de negocio e seguranca

1. Todos os endpoints exigem sessao (`[Authorize]`); sem cookie valido -> 401.
2. `UserId` vem do `ClaimTypes.NameIdentifier`, nunca do JSON nem da URL.
3. Consultas, edicoes e eliminacoes filtram por `UserId`, impedindo acesso cruzado.
4. `amount` e estritamente positivo, com no maximo duas casas decimais;
   o `type` da transacao e independente do tipo da categoria.
5. O `type` deve ser valido; `accountId` e `categoryId` nao podem ser vazios.
6. `date` e obrigatoria; `description` e opcional (max. 500 caracteres),
   sem espacos exteriores quando guardada.
7. Conta/categoria inexistente ou de outra pessoa -> 404.
8. Montante invalido ou periodo invertido -> 400.
9. Um ID de transacao inexistente ou de outro utilizador -> 404.

O modelo atual guarda montantes positivos para ambos os tipos; o sinal de uma
despesa e aplicado quando se calculam saldos/relatorios, nao na coluna `Amount`.

## 5. Como executar localmente (PowerShell)

Na raiz `personal-finance-app`:

```powershell
# Branch de trabalho, caso ainda nao estejas nele
# git switch feature/crud-transacoes

docker compose up -d
dotnet restore PersonalFinance.slnx
dotnet build PersonalFinance.slnx
dotnet test backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj --filter "FullyQualifiedName~TransactionsControllerTests|FullyQualifiedName~SwaggerEndpointsTests"
```

Se a base de dados ainda nao tiver as tabelas, apos configurar User Secrets:

```powershell
dotnet tool restore
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

Para preparar os utilizadores/contas/categorias de demonstracao, executar
`--seed` **apenas quando for preciso popular o ambiente Development**:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http -- --seed
```

Se o seed ja foi executado, o arranque normal e:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

Abrir `http://localhost:5185/swagger`. A rota `/auth/login` permite iniciar
sessao com os utilizadores de demonstracao. O Swagger pode exigir configurar
o cookie de autenticacao no navegador; alternativamente utiliza o script abaixo.

### Teste HTTP fim a fim

Num **segundo terminal**, com a API a correr:

```powershell
.\scripts\test-transactions.ps1
```

O script faz login em `/auth/login`, usa a conta e as categorias de demo1,
cria e altera uma transacao, verifica listagem e filtros, tenta operacoes
invalidas, confirma isolamento de demo2 e elimina a transacao temporaria.

**Dependencia de outra tarefa:** ainda nao existe `GET /api/categories` publico
neste ZIP. Ate o CRUD de categorias ser disponibilizado, para obter `categoryId`
no ambiente Development usa-se `/dev/users/{userId}/categories` ou DBeaver.
Nao utilizar as rotas `/dev` no frontend de producao.

## 6. Testes, integracao e Git

```powershell
dotnet test PersonalFinance.slnx

git status
git add backend/PersonalFinance.Api/Controllers/TransactionsController.cs `
  backend/PersonalFinance.Api/DTOs/Transactions `
  backend/PersonalFinance.Api/Program.cs `
  backend/PersonalFinance.Tests/TransactionsControllerTests.cs `
  backend/PersonalFinance.Tests/SwaggerEndpointsTests.cs `
  scripts/test-transactions.ps1 `
  docs/CRUD_TRANSACTIONS_GUIA.md

git commit -m "feat: implement transactions CRUD and date filters"
git push -u origin feature/crud-transacoes
```

**Nao enviar `.env`, User Secrets, `bin/` ou `obj/` ao Git.**

## 7. Por que estas opcoes?

- **Controller:** faz a ponte entre HTTP e EF Core, tal como no CRUD de contas.
- **DTOs:** validam entrada e nao permitem alterar `UserId`.
- **EF Core:** aplica `Where` por utilizador antes de aceder aos dados.
- **DateOnly + intervalo semiaberto:** facilita filtros por calendario sem
  excluir transacoes efetuadas a noite do ultimo dia.
- **`DateTimeOffset` no pedido:** converte offsets recebidos para UTC, compativel
  com a coluna PostgreSQL `timestamp with time zone`.
- **Sem nova migration:** esta tarefa adiciona API, nao altera o esquema da BD.
- **Testes InMemory:** cobrem logica sem depender de Docker; o script PowerShell
  exercita os pedidos HTTP reais contra PostgreSQL.
