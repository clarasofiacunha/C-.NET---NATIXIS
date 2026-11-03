# Personal Finance App

Aplicação web para **gestão de finanças pessoais**, desenvolvida em equipa. O objetivo é permitir que cada utilizador acompanhe contas, receitas, despesas, orçamentos mensais e metas de poupança num único local.

> **Estado do projeto:** o backend já inclui autenticação, CRUD de contas, CRUD de transações, CRUD de orçamentos, alertas de orçamento, Swagger, testes xUnit, migrations e dados de demonstração. O **frontend ainda contém o ecrã inicial do Vite/React**, não uma interface financeira completa. As categorias, metas e dados de dashboard existem na base de dados/rotas de diagnóstico, mas isso **não significa que os respetivos módulos públicos estejam concluídos**.

## Índice

1. [O que é a aplicação?](#1-o-que-é-a-aplicação)
2. [Tecnologias utilizadas](#2-tecnologias-utilizadas)
3. [Instalar as ferramentas necessárias](#3-instalar-as-ferramentas-necessárias)
4. [Preparar e executar o projeto pela primeira vez](#4-preparar-e-executar-o-projeto-pela-primeira-vez)
5. [Estrutura e responsabilidades das pastas](#5-estrutura-e-responsabilidades-das-pastas)
6. [Como trabalhar no projeto](#6-como-trabalhar-no-projeto)
7. [Utilizar a API, o Swagger e os testes](#7-utilizar-a-api-o-swagger-e-os-testes)
8. [Problemas frequentes e soluções](#8-problemas-frequentes-e-soluções)
9. [Funcionalidades planeadas](#9-funcionalidades-planeadas)
10. [Como criar a estrutura inicial (apenas uma vez)](#10-como-criar-a-estrutura-inicial-apenas-uma-vez)
11. [Checklist de instalação e execução](#11-checklist-de-instalação-e-execução)

---

## 1. O que é a aplicação?

A **Personal Finance App** será uma aplicação web que permite:

- Criar uma conta de utilizador e iniciar sessão.
- Gerir contas financeiras (por exemplo, conta à ordem e conta poupança).
- Registar receitas e despesas e consultar o histórico.
- Organizar transações por categorias.
- Definir orçamentos mensais por categoria.
- Acompanhar saldos e indicadores num dashboard.
- Criar e acompanhar metas de poupança.

**Importante:** nem todas estas funcionalidades estarão disponíveis logo após instalar o projeto. Serão desenvolvidas progressivamente. A primeira versão não prevê integração com bancos nem aplicação móvel.

### Como funciona?

```text
Navegador (React ou Swagger UI)
    |
    | Pedido HTTP /auth/... ou /api/...
    v
Backend (ASP.NET Core Web API)
    |
    | Entity Framework Core
    v
Base de dados PostgreSQL (Docker)
```

Em desenvolvimento, React e .NET executam no computador; o PostgreSQL executa num contentor Docker.

---

## 2. Tecnologias utilizadas

| Tecnologia | Para que serve | Versão de referência |
| --- | --- | --- |
| **React** | Construir as páginas e componentes | A definida em `frontend/package.json` |
| **TypeScript** | Adicionar tipos ao código JavaScript | A definida em `frontend/package.json` |
| **Vite** | Iniciar e compilar o frontend | A definida em `frontend/package.json` |
| **ESLint** | Analisar problemas e manter regras de código | A definida no frontend |
| **C# / ASP.NET Core** | Criar a API e as regras de negócio | **.NET SDK 10** |
| **Entity Framework Core** | Ler, gravar e atualizar dados | **EF Core 8.x** no projeto atual |
| **PostgreSQL** | Guardar dados | **PostgreSQL 17** via Docker |
| **Docker Desktop / Compose** | Executar a base de dados localmente | Versão estável |
| **Git e GitHub** | Partilhar e versionar código | Versão estável |
| **xUnit** | Executar testes do backend | A definida no projeto de testes |
| **Swagger / Swashbuckle** | Documentar e experimentar os endpoints HTTP em desenvolvimento | Configurado em `Program.cs` |
| **ASP.NET Core Identity** | Registo, autenticação por cookie e proteção dos dados | Configurado no backend |
| **DBeaver** (opcional) | Consultar visualmente as tabelas PostgreSQL | Cliente SQL; não substitui a API |

> Não precisas de instalar o PostgreSQL diretamente no Windows: o Docker fará isso dentro de um contentor.

---

## 3. Instalar as ferramentas necessárias

As instruções abaixo são para **Windows 10/11, usando PowerShell**. Em macOS/Linux, os princípios são os mesmos, mas alguns comandos de instalação e cópia de ficheiros mudam.

### 3.1. Git

1. Abre <https://git-scm.com/downloads>.
2. Instala o **Git for Windows**, usando as opções padrão se não tiveres requisitos específicos.
3. Abre o **PowerShell** (menu Iniciar → procura `PowerShell`).
4. Executa:

```powershell
git --version
```

Deverás ver um número de versão. O Git permite descarregar o código do GitHub e enviar as alterações.

Opcionalmente, se ainda não configuraste o teu nome e email para commits:

```powershell
git config --global user.name "O Teu Nome"
git config --global user.email "o-teu-email@example.com"
```

Utiliza o email associado à tua conta GitHub, caso pretendas associar os commits ao perfil.

### 3.2. Visual Studio Code

1. Abre <https://code.visualstudio.com/>.
2. Instala o **Visual Studio Code**.
3. Extensões úteis (não obrigatórias): **C# Dev Kit**, **ESLint**, **Docker** e **Prettier**.
4. Para abrir o projeto, utiliza **File → Open Folder** e escolhe a pasta `personal-finance-app`.

### 3.3. .NET SDK 10

1. Abre <https://dotnet.microsoft.com/download>.
2. Descarrega e instala o **SDK .NET 10** (não apenas o Runtime).
3. Fecha e reabre o PowerShell.
4. Confirma:

```powershell
dotnet --version
```

O resultado deverá começar por `10.`. Se o comando não existir, reinicia o terminal ou o computador e confirma que o SDK foi instalado.

### 3.4. Node.js e npm

1. Abre <https://nodejs.org/>.
2. Instala **Node.js 24 LTS**.
3. O `npm` é instalado juntamente com o Node.js.
4. Confirma:

```powershell
node --version
npm --version
```

A versão de Node.js deverá começar por `v24.`. O npm será usado para instalar as dependências do React.

### 3.5. Docker Desktop e WSL 2

1. Abre <https://www.docker.com/products/docker-desktop/>.
2. Instala o **Docker Desktop**.
3. Abre o Docker Desktop e aguarda até o motor Docker estar pronto.
4. Confirma:

```powershell
docker --version
docker compose version
docker run --rm hello-world
```

O último comando deverá apresentar a mensagem `Hello from Docker!`.

**Se o instalador apresentar o erro _Virtual Machine Platform is not enabled_:**

1. Fecha o instalador.
2. Abre o **PowerShell como administrador** (botão direito → Executar como administrador).
3. Executa:

```powershell
wsl --install
```

4. **Reinicia o computador**.
5. Confirma o estado com `wsl --status` e, se necessário, executa `wsl --set-default-version 2`.
6. Tenta instalar/abrir o Docker Desktop novamente. Nas definições do Docker, utiliza o motor baseado em **WSL 2**, quando disponível.

Se o WSL já existir mas a funcionalidade continuar desativada, podes executar no PowerShell **como administrador**:

```powershell
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
```

Depois, reinicia. Em alguns computadores também pode ser necessário ativar a virtualização nas definições BIOS/UEFI; em equipamentos da escola/empresa pode ser preciso pedir ajuda ao administrador.

### 3.6. Confirmar tudo de uma vez

```powershell
git --version
dotnet --version
node --version
npm --version
docker --version
docker compose version
```

Se algum comando não funcionar, resolve esse problema antes de avançar.

---

## 4. Preparar e executar o projeto pela primeira vez

> **Atenção:** os comandos desta secção partem do princípio de que a estrutura inicial já foi criada e enviada para um **repositório de código GitHub**. O endereço de **GitHub Projects** (`https://github.com/users/clarasofiacunha/projects/6`) é o quadro de tarefas, **não** o endereço para `git clone`.

### Passo 1 — Obter o código

1. Abre o repositório de código no GitHub (não o quadro de tarefas).
2. Clica no botão **Code → HTTPS** e copia o URL.
3. Abre o PowerShell na pasta onde queres guardar o projeto.
4. Executa (se ainda não tiveres o repositório clonado):

```powershell
git clone https://github.com/clarasofiacunha/C-.NET---NATIXIS.git
cd C-.NET---NATIXIS\personal-finance-app
```

Se o repositório for privado, a tua conta GitHub precisa de ter acesso. O Git pode pedir autenticação através do navegador ou gestor de credenciais.

**Todas as instruções seguintes devem ser executadas na pasta raiz `personal-finance-app`, salvo quando indicado o contrário.**

### Passo 2 — Configurar a base de dados

Confirma que existe o ficheiro `.env.example` na raiz. Faz uma cópia com o nome `.env`:

```powershell
Copy-Item .env.example .env
```

Abre `.env` no VS Code. O conteúdo deve seguir este modelo:

```dotenv
POSTGRES_DB=personal_finance
POSTGRES_USER=personal_finance_user
POSTGRES_PASSWORD=change_this_password
POSTGRES_PORT=5432
```

Altera `POSTGRES_PASSWORD` para uma password tua, usada apenas no desenvolvimento local. Estes valores são os exemplos do ficheiro `.env.example`; se o teu PostgreSQL já foi inicializado com outro utilizador, utiliza **as credenciais efetivas** da tua base de dados. **Não partilhes o `.env` nem o publiques no GitHub.**

O repositório analisado já inclui `.env.example` e `docker-compose.yml`. Se não os encontrares, confirma que abriste a raiz correta e que tens a versão atualizada da branch antes de prosseguir.

### Passo 3 — Ligar o PostgreSQL

Abre o **Docker Desktop**. Na raiz do projeto, executa:

```powershell
docker compose up -d
docker compose ps
```

Aguarda até o contentor `personal-finance-db` mostrar o estado **healthy**. O PostgreSQL estará acessível no computador na porta `5432`.

> Se a porta `5432` já estiver ocupada, fala com a equipa antes de mudar a porta para que a ligação da API continue correta.

### Passo 4 — Preparar o backend

Confirma que existe `backend/PersonalFinance.Api/PersonalFinance.Api.csproj` e, na raiz do repositório, executa:

```powershell
dotnet restore PersonalFinance.slnx
dotnet tool restore --tool-manifest ./dotnet-tools.json
```

O segundo comando restaura o `dotnet-ef` **8.0.33** definido no manifesto `dotnet-tools.json` da raiz. O backend utiliza .NET 10 como *target framework*, mas as dependências EF Core/Npgsql deste código estão na versão **8.x**. Não alteres versões de pacotes isoladamente sem coordenação com a equipa.

**Configurar a ligação à base de dados sem guardar passwords no código:**

O ficheiro `.csproj` do backend **já contém** `UserSecretsId`, por isso não é necessário executar `dotnet user-secrets init` novamente. Define a ligação numa única linha, utilizando os valores **reais** da tua base de dados:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Host=localhost;Port=5432;Database=personal_finance;Username=personal_finance_user;Password=A_TUA_PASSWORD' --project backend/PersonalFinance.Api
```

Se usares outra porta, username ou base de dados, adapta também a connection string. O DBeaver deve usar esses mesmos dados de ligação.

Usamos **User Secrets** para a password não aparecer no código versionado. Cada colega deve configurar os seus próprios segredos na respetiva máquina.

**Aplicar as migrations da base de dados:**

```powershell
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

A migration inicial **já existe** em `backend/PersonalFinance.Api/Migrations/`. Cada pessoa deve aplicar as migrations do repositório; **não** criar novamente `InitialCreate`. O CRUD de transações reutiliza a tabela existente, pelo que não precisa de uma migration nova.

**Popular a base de dados com dados fictícios (opcional, apenas em desenvolvimento):**

O `DemoSeeder` **já está implementado**. Depois de aplicar as migrations, executa, com a porta da API livre:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http -- --seed
```

Este comando aplica o seed **e mantém a API em execução**. Termina-a com `Ctrl + C` se quiseres voltar a iniciá-la sem `--seed`. Não precisas de repetir o seed em cada arranque.

O seed permite criar dados de demonstração, incluindo:

- **4 utilizadores** fictícios (`demo1@example.test` a `demo4@example.test`), com password de desenvolvimento `Demo123!Change`.
- Contas à ordem e contas poupança.
- Categorias de receitas e despesas.
- Transações distribuídas ao longo de 12 meses.
- Orçamentos mensais por categoria.
- Metas de poupança.

Os dados são sintéticos e servem para testar funcionalidades
como filtros, dashboard, orçamentos e relatórios.

O comando deve ser executado apenas em ambiente de
desenvolvimento. Não utilizar contas ou passwords de
demonstração em produção.

**Compilar o backend:**

```powershell
dotnet build PersonalFinance.slnx
```

### Passo 5 — Instalar as dependências do frontend

```powershell
cd frontend
npm ci
cd ..
```

`npm ci` utiliza o ficheiro `package-lock.json` versionado e instala as dependências nas versões combinadas pela equipa. Neste projeto o `package-lock.json` já existe; executa `npm ci`.

### Passo 6 — Iniciar a aplicação (três terminais)

**Terminal 1 — Base de dados** (na raiz):

```powershell
docker compose up -d
```

**Terminal 2 — Backend** (na raiz):

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

**Terminal 3 — Frontend**:

```powershell
cd frontend
npm run dev
```

Deixa os terminais do backend e frontend abertos enquanto estiveres a usar a aplicação. Para parar um destes servidores, usa `Ctrl + C` no respetivo terminal.

### Passo 7 — Abrir e confirmar

- **Frontend (ainda com o template inicial do Vite):** <http://localhost:5173>
- **API:** <http://localhost:5185/> — devolve o estado geral da API.
- **Health check:** <http://localhost:5185/health> — testa também a ligação ao PostgreSQL.
- **Swagger UI:** <http://localhost:5185/swagger> — documentação e testes interativos (apenas Development).

Resposta esperada de `GET /health` quando a API e o PostgreSQL estão operacionais:

```json
{
  "api": "online",
  "database": "connected"
}
```

O endpoint `/health` pode devolver **503** quando a API está online mas o PostgreSQL está indisponível. Um erro de ligação ao endereço indica que a própria API não está a ouvir nessa porta. O frontend ainda não tem os módulos de gestão de finanças implementados; o Swagger permite experimentar diretamente o backend.

### Passo 8 — Parar tudo

- Para parar o React ou a API, usa `Ctrl + C` em cada terminal.
- Para parar e remover o contentor PostgreSQL **sem apagar o volume de dados**, executa na raiz:

```powershell
docker compose down
```

**Não executes `docker compose down -v`** a menos que saibas que estás a eliminar intencionalmente os dados locais.

### No dia seguinte, como voltar a abrir?

Não precisas de reinstalar tudo. Abre o Docker Desktop, abre o projeto e repete apenas os **três terminais do Passo 6**.

---

## 5. Estrutura e responsabilidades das pastas

Esta é uma **visão simplificada da estrutura que já existe**, sem incluir `bin/`, `obj/`, `node_modules/` e ficheiros gerados automaticamente:

```text
personal-finance-app/
├── backend/
│   ├── PersonalFinance.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs          # Registo, login, perfil e logout
│   │   │   ├── AccountsController.cs      # CRUD de contas
│   │   │   ├── TransactionsController.cs  # CRUD de transações e filtros
│   │   │   ├── BudgetsController.cs       # CRUD de orçamentos e alertas
│   │   │   ├── HealthController.cs        # / e /health
│   │   │   └── DevelopmentController.cs   # Endpoints /dev/*
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   ├── Accounts/
│   │   │   ├── Transactions/
│   │   │   └── Budgets/
│   │   ├── Services/                      # Lógica de orçamentos
│   │   ├── Models/                        # Entidades Account, Category, etc.
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs            # EF Core + tabelas/relações
│   │   │   └── DemoSeeder.cs             # Dados fictícios de desenvolvimento
│   │   ├── Migrations/                   # InitialCreate existente
│   │   ├── Properties/launchSettings.json
│   │   ├── Program.cs                    # Serviços, auth, CORS e Swagger
│   │   └── PersonalFinance.Api.csproj
│   └── PersonalFinance.Tests/            # Testes xUnit
│       ├── RegisterTests.cs / LoginTests.cs
│       ├── AccountsControllerTests.cs
│       ├── TransactionsControllerTests.cs
│       ├── BudgetRequestValidationTests.cs
│       ├── BudgetAlertClassifierTests.cs
│       ├── SwaggerEndpointsTests.cs
│       └── PersonalFinance.Tests.csproj
├── frontend/
│   ├── src/                              # App.tsx, estilos e assets iniciais
│   ├── package.json
│   └── vite.config.ts                    # Configuração Vite (sem proxy /api)
├── scripts/
│   ├── test-accounts.ps1
│   └── test-transactions.ps1
├── docs/                                 # Guias de setup, contas e transações
├── docker-compose.yml                   # PostgreSQL
├── .env.example                         # Variáveis de ambiente de exemplo
├── .env                                 # Valores privados locais (não versionar)
├── dotnet-tools.json                    # Manifesto de ferramentas (.NET EF)
├── PersonalFinance.slnx
└── README.md
```

### Backend: que responsabilidade tem cada parte?

| Pasta/ficheiro | Função | Exemplo |
| --- | --- | --- |
| `Controllers/` | Expõe as rotas HTTP e aplica as regras de acesso. | `POST /api/transactions` |
| `DTOs/` | Define os contratos JSON de entrada e saída e as validações. | `TransactionRequestDto` |
| `Services/` | Isola a lógica de negócio que não deve ficar no controller. | `BudgetService` |
| `Models/` | Define entidades, enums e relações da aplicação. | `FinanceTransaction`, `TransactionType` |
| `Data/AppDbContext.cs` | Configura o Entity Framework Core/PostgreSQL. | Tabelas `Accounts` e `Transactions` |
| `Data/DemoSeeder.cs` | Insere utilizadores e dados fictícios locais. | `--seed` |
| `Migrations/` | Histórico versionado do esquema da base de dados. | `InitialCreate` |
| `Program.cs` | Configura Identity, cookies, CORS, DI, JSON e Swagger. | Autenticação e `AddSwaggerGen()` |
| `PersonalFinance.Tests/` | Testes automatizados isolados, incluindo integração do Swagger. | `TransactionsControllerTests` |
| `scripts/` | Testes HTTP de ponta a ponta contra a API em execução. | `test-transactions.ps1` |

**Regras importantes:**

- Os pedidos de contas, transações e orçamentos são associados ao **utilizador autenticado**, não a um `userId` enviado no JSON.
- Os DTOs evitam expor diretamente as entidades internas e aplicam validações.
- Para valores financeiros utiliza-se `decimal` em C#.
- Nunca guardar passwords, cookies de sessão, connection strings ou `.env` no repositório.
- O frontend atual é apenas um ponto de partida; **não apresenta ainda uma interface funcional para estes CRUDs**.
- O `vite.config.ts` atual **não configura um proxy `/api`**. Ao adicionar chamadas HTTP no React, configurar o URL da API e enviar os cookies (`credentials: 'include'`), respeitando as origens CORS previstas.

### Ficheiros da raiz

| Ficheiro | Para que serve |
| --- | --- |
| `README.md` | Este manual de instalação e utilização. |
| `PersonalFinance.slnx` | Agrupa os projetos .NET. |
| `docker-compose.yml` | Define o serviço PostgreSQL (`db`) e o volume. |
| `.env.example` | Exemplo versionável das variáveis usadas pelo Docker. |
| `.env` | Configuração privada da máquina; não partilhar. |
| `dotnet-tools.json` | Define `dotnet-ef` 8.0.33. |
| `.gitignore` | Exclui segredos e ficheiros de compilação. |

---

## 6. Como trabalhar no projeto

### 6.1. Começar a trabalhar

Antes de desenvolver, guarda ou confirma o teu trabalho com `git status`, muda para a branch base combinada pela equipa (por exemplo, `main`) e obtém as alterações mais recentes:

```powershell
git switch main
git pull origin main
git switch -c feature/nome-da-tarefa
```

Se já tens uma branch própria, **não voltes a criá-la**: utiliza `git switch feature/nome-da-tarefa` e atualiza-a em coordenação com a equipa.

Depois de implementar e executar os testes:

```powershell
git status
git add .
git commit -m "docs: update API and testing instructions"
git push -u origin feature/nome-da-tarefa
```

Abre um **Pull Request** para a branch de integração acordada (por exemplo, `main` ou `develop`) e pede revisão. Confirma sempre a branch de destino antes do merge.

### 6.2. Convenções simples

- **Nomes de pastas e classes:** em inglês (`AccountsController`, `SavingsGoal`).
- **Commits:** descritivos, por exemplo `feat: add transactions` ou `fix: correct balance calculation`.
- **Uma branch por funcionalidade/correção**; evita que todos editem diretamente a `main`.
- **Nunca partilhar credenciais reais** nas Issues, código ou Pull Requests.
- **Não alterar migrations já aplicadas por toda a equipa** sem discutir com os responsáveis pelo backend.
- **Não gerar novamente** os projetos com `dotnet new` ou `npm create vite` se eles já existirem no repositório.

### 6.3. Gestão das tarefas

Quadro da equipa: <https://github.com/users/clarasofiacunha/projects/6>

O **GitHub Project** organiza tarefas; as **Issues** descrevem o trabalho e os **Pull Requests** propõem alterações ao código. O repositório terá um URL próprio (diferente do URL do quadro).

---

## 7. Utilizar a API, o Swagger e os testes

> Todos os exemplos abaixo assumem **PowerShell no Windows** e que o terminal está na **raiz `personal-finance-app`**, exceto quando indicado. O endereço padrão da API é `http://localhost:5185` (perfil `http` em `launchSettings.json`).

### 7.1. Iniciar e verificar a API

**Terminal 1 — PostgreSQL:**

```powershell
docker compose up -d
docker compose ps
```

**Terminal 2 — API .NET:**

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

Mantém o terminal da API aberto. Confirma no navegador:

- `http://localhost:5185/` — API online.
- `http://localhost:5185/health` — ligação ao PostgreSQL.
- `http://localhost:5185/swagger` — Swagger UI.
- `http://localhost:5185/swagger/v1/swagger.json` — especificação OpenAPI em JSON.

Se a base de dados ainda estiver vazia, aplica as migrations e executa o seed de acordo com a [secção 4](#4-preparar-e-executar-o-projeto-pela-primeira-vez). **A ligação no DBeaver confirma o PostgreSQL, mas não inicia a API .NET.**

### 7.2. O que é o Swagger e como o utilizar

O **Swagger UI** é uma interface web para ler a documentação gerada pela API e enviar pedidos HTTP sem criar páginas React nem instalar Postman.

1. Inicia a API com `--launch-profile http` e abre `http://localhost:5185/swagger`.
2. Expande um endpoint (por exemplo, `POST /auth/login`).
3. Clica em **Try it out**.
4. Preenche os parâmetros ou o corpo JSON indicado na documentação.
5. Clica em **Execute**.
6. Consulta **Request URL**, **Server response**, **Response body** e **Response code**.
7. Para testar uma rota protegida, faz primeiro login no mesmo Swagger (ver [7.3](#73-autenticação-e-sessão)). O navegador guarda o cookie devolvido pelo login e envia-o nos pedidos seguintes à mesma origem.

O Swagger está configurado **apenas em Development**; não fica disponível em Production. No código atual, a autenticação é feita por **cookie de sessão**, não por token JWT. Não é necessário inventar ou introduzir um Bearer token no Swagger.

### 7.3. Autenticação e sessão

As rotas de autenticação ficam em `/auth` (sem o prefixo `/api`).

| Método | Endpoint | Função | Sucesso |
| --- | --- | --- | --- |
| `POST` | `/auth/register` | Registar utilizador | `201` |
| `POST` | `/auth/login` | Iniciar sessão, recebe cookie `pf.auth` | `200` |
| `GET` | `/auth/me` | Consultar dados do utilizador autenticado | `200` |
| `POST` | `/auth/logout` | Terminar sessão | `204` |

**Exemplo de registo** (Swagger → `POST /auth/register` → Try it out):

```json
{
  "name": "Utilizador Teste",
  "email": "utilizador.teste@example.test",
  "password": "PasswordTeste123!"
}
```

**Exemplo de login** (Swagger → `POST /auth/login`):

```json
{
  "email": "demo1@example.test",
  "password": "Demo123!Change"
}
```

As contas `demo1@example.test` a `demo4@example.test` são criadas pelo `--seed`; não existem obrigatoriamente antes desse comando. A password acima é **exclusivamente de desenvolvimento**.

Depois de um `200` no login, testa `GET /auth/me`, `GET /api/accounts` ou `GET /api/transactions` no mesmo navegador. O backend aplica `[Authorize]` aos CRUDs. Sem sessão, devolve `401`; os recursos de outro utilizador são ocultados (`404`). O cookie `pf.auth` é `HttpOnly`, por isso não se deve tentar lê-lo com JavaScript.

**Alternativa em PowerShell** (a sessão conserva o cookie):

```powershell
$base = 'http://localhost:5185'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$login = Invoke-RestMethod -Method Post -Uri "$base/auth/login" `
  -ContentType 'application/json' `
  -Body '{"email":"demo1@example.test","password":"Demo123!Change"}' `
  -WebSession $session

$login
Invoke-RestMethod -Uri "$base/auth/me" -WebSession $session
Invoke-RestMethod -Uri "$base/api/accounts" -WebSession $session
```

Nos pedidos posteriores pelo PowerShell, acrescenta `-WebSession $session`. No frontend React, as chamadas com `fetch` devem incluir `credentials: 'include'` se estiverem numa origem diferente da API e a configuração CORS for compatível.

### 7.4. CRUD de contas financeiras

| Método | Endpoint | Resultado esperado |
| --- | --- | --- |
| `GET` | `/api/accounts` | `200` — contas do utilizador autenticado |
| `GET` | `/api/accounts/{id}` | `200` — conta encontrada |
| `POST` | `/api/accounts` | `201` — conta criada |
| `PUT` | `/api/accounts/{id}` | `200` — conta atualizada |
| `DELETE` | `/api/accounts/{id}` | `204` — conta eliminada, se não tiver transações |

Exemplo JSON para `POST` ou `PUT`:

```json
{
  "name": "Poupança de emergência",
  "type": "Savings",
  "initialBalance": 250.75
}
```

Tipos de conta aceites: `Checking`, `Savings` e `Cash`. O nome é obrigatório (até 120 caracteres) e o saldo inicial é um número decimal com, no máximo, duas casas. O ID da conta é um **GUID**. Não é permitido apagar uma conta que tem transações associadas: a API responde `409 Conflict`.

Depois de criares uma conta no Swagger, copia o `id` devolvido e utiliza-o nos endpoints `GET /api/accounts/{id}`, `PUT` e `DELETE`.

### 7.5. CRUD de transações (receitas e despesas)

| Método | Endpoint | Resultado esperado |
| --- | --- | --- |
| `GET` | `/api/transactions` | `200` — transações do utilizador |
| `GET` | `/api/transactions/{id}` | `200` — transação encontrada |
| `POST` | `/api/transactions` | `201` — transação criada |
| `PUT` | `/api/transactions/{id}` | `200` — transação atualizada |
| `DELETE` | `/api/transactions/{id}` | `204` — transação eliminada |

**Filtros opcionais de `GET /api/transactions`:**

| Parâmetro | Exemplo | Significado |
| --- | --- | --- |
| `from` | `2026-11-01` | Data inicial, inclusive (UTC) |
| `to` | `2026-11-30` | Data final, inclusive (UTC) |
| `type` | `Expense` ou `Income` | Apenas despesas ou receitas |
| `accountId` | GUID de conta | Apenas transações dessa conta |
| `categoryId` | GUID de categoria | Apenas transações dessa categoria |

Exemplo: `GET /api/transactions?from=2026-11-01&to=2026-11-30&type=Expense`. Por omissão, os resultados são ordenados **do mais recente para o mais antigo**.

**JSON para criar/editar uma despesa:**

```json
{
  "accountId": "COLOCAR_GUID_DA_CONTA",
  "categoryId": "COLOCAR_GUID_DE_CATEGORIA_EXPENSE",
  "amount": 24.50,
  "type": "Expense",
  "date": "2026-11-03T12:30:00Z",
  "description": "Supermercado"
}
```

Substitui os dois campos pelos **GUIDs verdadeiros**, pois os valores de exemplo acima são marcadores e não GUIDs válidos. Para descobrir os IDs, utiliza `GET /api/accounts` e, enquanto o CRUD público de categorias não estiver disponível, `GET /dev/users/{userId}/categories` (apenas Development; ver [7.7](#77-base-de-dados-dbeaver-e-endpoints-de-desenvolvimento)).

**Regras implementadas:**

- `Income` = receita; `Expense` = despesa. O montante é **sempre positivo** e pode ter, no máximo, duas casas decimais.
- A categoria tem de pertencer ao utilizador autenticado **e** ter o mesmo tipo da transação.
- Conta e categoria têm de pertencer ao utilizador autenticado; não é possível escrever o `userId` no JSON.
- Data obrigatória em ISO 8601 com `Z` (UTC) ou offset (por exemplo, `+01:00`); o backend guarda-a em UTC.
- Descrição opcional, até 500 caracteres; espaços exteriores são removidos.
- Datas invertidas, valores inválidos ou categoria de tipo incompatível: `400`. Recurso inexistente ou de outro utilizador: `404`.

**Passos no Swagger:** faz login → obtém `accountId` e `categoryId` → executa `POST /api/transactions` → guarda o `id` devolvido → verifica com `GET` → edita com `PUT` → remove com `DELETE` → confirma `404` numa nova consulta.

### 7.6. Orçamentos mensais e alertas

| Método | Endpoint | Função |
| --- | --- | --- |
| `GET` | `/api/budgets` | Lista orçamentos, com filtros opcionais `year` e `month` |
| `GET` | `/api/budgets/{id}` | Consulta um orçamento |
| `POST` | `/api/budgets` | Cria um orçamento |
| `PUT` | `/api/budgets/{id}` | Atualiza um orçamento |
| `DELETE` | `/api/budgets/{id}` | Elimina um orçamento |
| `GET` | `/api/budgets/alerts` | Lista alertas de consumo, com filtros `year` e `month` |

Exemplo JSON para criar ou editar um orçamento:

```json
{
  "categoryId": "COLOCAR_GUID_DE_CATEGORIA_EXPENSE",
  "month": 11,
  "year": 2026,
  "limitAmount": 250.00
}
```

Os orçamentos aplicam-se a **categorias de despesa do próprio utilizador**. O limite tem de ser positivo, o mês tem de estar entre 1 e 12 e não pode existir mais de um orçamento para a mesma categoria/ano/mês (duplicado → `409 Conflict`).

O endpoint de alertas apresenta categorias com gastos **a partir de 80% do limite** (`NearLimit`), ou que já **ultrapassaram 100%** (`Exceeded`). Inclui o limite, o montante gasto, o saldo restante e a percentagem de utilização. O cálculo utiliza as transações do utilizador da categoria e do mês correspondente. Trata-se de **alertas disponibilizados pela API**, não de notificações automáticas ou de uma interface frontend já concluída.

### 7.7. Base de dados, DBeaver e endpoints de desenvolvimento

**DBeaver** é opcional. Para visualizar as tabelas, cria uma ligação PostgreSQL com host `localhost`, porta definida em `POSTGRES_PORT` (por omissão `5432`), database/username/password iguais aos configurados no PostgreSQL; a base e o utilizador podem ser diferentes dos valores de exemplo. Na ligação, abre `Schemas → public → Tables → Accounts / Transactions / Categories / Budgets` e consulta a aba **Data**. Evita editar ou apagar diretamente os registos de desenvolvimento, salvo quando souberes o efeito.

As rotas `/dev/*` só devolvem dados no ambiente **Development**. Exemplos:

| Endpoint | Para que serve |
| --- | --- |
| `GET /dev/db` | Quantidade de utilizadores, contas, categorias, transações, orçamentos e metas |
| `GET /dev/users` | IDs e emails de demonstração |
| `GET /dev/users/{userId}/accounts` | Contas do utilizador indicado |
| `GET /dev/users/{userId}/categories` | IDs e tipos de categorias (útil para o CRUD de transações) |
| `GET /dev/users/{userId}/transactions` | Até 50 transações recentes desse utilizador |
| `GET /dev/users/{userId}/budgets` | Orçamentos desse utilizador |
| `GET /dev/users/{userId}/goals` | Metas desse utilizador |
| `GET /dev/users/{userId}/dashboard?year=2026&month=11` | Resumo mensal de receitas, despesas e limites |

**Atenção:** estes endpoints de diagnóstico mostram dados por ID e **não substituem APIs públicas protegidas por autenticação**. Só devem ser usados em ambiente de desenvolvimento. O CRUD público de categorias, o dashboard público e o CRUD de metas **não estão implementados no código analisado**.

### 7.8. Testes automatizados com xUnit

Os testes xUnit são executados pelo SDK .NET, sem precisar de iniciar manualmente a API. Vários testes do CRUD utilizam a base InMemory em vez de PostgreSQL, enquanto testes do Swagger verificam o comportamento da aplicação de teste.

Na raiz do projeto:

```powershell
# Compilar o código
dotnet build PersonalFinance.slnx

# Executar todos os testes automatizados
dotnet test backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj

# Apenas testes do CRUD de contas e Swagger
dotnet test backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj --filter "FullyQualifiedName~AccountsControllerTests|FullyQualifiedName~SwaggerEndpointsTests"

# Apenas testes de transações e Swagger
dotnet test backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj --filter "FullyQualifiedName~TransactionsControllerTests|FullyQualifiedName~SwaggerEndpointsTests"

# Também podes executar testes da solução
dotnet test PersonalFinance.slnx
```

O projeto contém ainda testes de registo, login, validação de pedidos de orçamento e classificação de alertas. **Um comando documentado não significa que os testes passaram:** confirma o resultado `Passed` no teu terminal e resolve eventuais falhas antes de abrir o Pull Request.

### 7.9. Testes HTTP de ponta a ponta em PowerShell

Ao contrário do xUnit, estes scripts exigem a **API a correr**, o **PostgreSQL ligado**, a migration aplicada e os **utilizadores do seed criados**.

**Terminal 1 (na raiz):**

```powershell
docker compose up -d
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

**Terminal 2 (na raiz):**

```powershell
.\scripts\test-transactions.ps1
.\scripts\test-accounts.ps1
```

O script `test-transactions.ps1` faz login, cria, consulta, filtra, atualiza e elimina uma transação de teste. Verifica também `401` sem sessão, `400` em pedidos inválidos e `404` entre utilizadores. Em caso de execução normal, apaga a transação temporária.

**Atenção ao script de contas:** na versão do projeto analisada, `scripts/test-accounts.ps1` ainda faz login em `/dev/auth/login`, que **não existe** no `AuthController` atual. Antes de o executar, substitui **as duas ocorrências** de `/dev/auth/login` por `/auth/login` nesse script. O restante teste cobre as operações CRUD de contas, validações, isolamento e bloqueio de eliminação de contas que tenham transações.

Se já estiveres na pasta `scripts`, executa `.\test-transactions.ps1` ou `.\test-accounts.ps1` (sem o prefixo `scripts/`). O PowerShell não executa scripts da pasta atual apenas pelo nome. Se surgir uma política de execução impeditiva, podes permitir apenas na sessão atual:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Caso uses outra porta, os scripts aceitam o argumento `-BaseUrl`:

```powershell
.\scripts\test-transactions.ps1 -BaseUrl 'http://localhost:5185'
```

**Testes HTTP têm efeitos na BD de desenvolvimento.** Não os executes contra produção nem contra uma base com dados reais.

### 7.10. Outras verificações

```powershell
# Frontend — análise estática e build
cd frontend
npm ci
npm run lint
npm run build
cd ..

# Base de dados — estado do container
docker compose ps
```

Na branch atual, o frontend continua a ser o template React/Vite. Um build do frontend sem erros não significa que as páginas de finanças estejam implementadas. Se surgir um erro num teste ou build, consulta a [secção 8](#8-problemas-frequentes-e-soluções).

---

## 8. Problemas frequentes e soluções

### `docker` não é reconhecido

Instala/inicia o Docker Desktop, fecha e reabre o PowerShell e confirma `docker --version`.

### Docker Desktop: `Virtual Machine Platform is not enabled`

Executa `wsl --install` num PowerShell **como administrador**, reinicia o Windows e volta a tentar. Consulta a [secção 3.5](#35-docker-desktop-e-wsl-2).

### `Cannot connect to the Docker daemon`

Abre o Docker Desktop e espera até estar pronto. Verifica se está a usar os contentores Linux/WSL 2.

### A porta `5432` já está em utilização

Já pode existir outro PostgreSQL no computador. Não elimines outros serviços sem saber para que servem. Altera a porta publicada no Docker Compose **e** a connection string, em coordenação com a equipa.

### `Password authentication failed` ou erro de ligação à base de dados

1. Confirma que o Docker está a funcionar: `docker compose ps`.
2. Confirma que o nome da base de dados, utilizador e password em `.env` correspondem à connection string guardada com `dotnet user-secrets`.
3. Se alteraste a password do `.env` **depois** de criar o contentor/volume, o PostgreSQL pode continuar com as credenciais iniciais. Pede ajuda antes de limpar volumes; limpar o volume apaga os dados locais.

**Erro: `28P01: password authentication failed`**

Significa que o PostgreSQL rejeitou as credenciais utilizadas
pelo backend.

1. Confirmar que o contentor está a correr:

```powershell
docker compose ps
```

2. Verificar os nomes dos serviços:

```powershell
docker compose config --services
```

Neste projeto, o serviço chama-se `db` e o contentor
chama-se `personal-finance-db`.

3. Testar a ligação ao PostgreSQL:

```powershell
docker compose exec db psql -U personal_finance_user -d personal_finance
```

4. Confirmar que a connection string dos User Secrets
utiliza o mesmo utilizador, base de dados e password
configurados no PostgreSQL.

**Erro: `service "postgres" is not running`**

O nome do serviço Compose é `db`, não `postgres`.

Utilizar `docker compose exec db ...` ou
`docker exec -it personal-finance-db ...`.

**Erro: `role "finance_dev" does not exist`**

O `.env.example` utiliza `personal_finance_user`, mas a tua base de dados já existente pode ter um utilizador diferente. Usa a role que realmente foi criada quando o volume PostgreSQL foi inicializado e ajusta a connection string dos User Secrets.

**Atenção aos volumes Docker:**

O volume `postgres_data` guarda os dados entre reinícios.
Alterar o `.env` não altera automaticamente os utilizadores
e as passwords de uma base de dados já inicializada.

Nunca executar `docker compose down -v` para corrigir
erros de autenticação sem confirmar que os dados podem
ser eliminados.

### `dotnet` não é reconhecido / versão errada

Instala o **.NET SDK 10**, fecha e reabre o terminal e confirma `dotnet --version`.

### `dotnet ef` não é reconhecido

Na raiz, executa `dotnet tool restore --tool-manifest ./dotnet-tools.json`. O manifesto já existe neste projeto e fixa `dotnet-ef` 8.0.33.

### `No migrations were found` ou pasta `Migrations/` em falta

A migration `InitialCreate` já está versionada. Confirma que estás a executar o comando na raiz do repositório e que tens os ficheiros de `Migrations/`. **Não recries a migration** sem coordenação com a equipa.

### `npm` não é reconhecido

Instala Node.js LTS e reabre o PowerShell. Verifica `node --version` e `npm --version`.

### `npm ci` falha por falta de `package-lock.json`

O responsável pelo frontend deverá executar `npm install` na pasta `frontend/` e enviar o `package-lock.json` para o repositório. Depois, os restantes elementos usam `npm ci`.

### O React abre, mas a API não responde

1. Confirma que a API .NET está a executar e a mostrar `Now listening on: http://localhost:5185`.
2. Abre `http://localhost:5185/health` num navegador.
3. O ficheiro `frontend/vite.config.ts` **não contém um proxy `/api`**; as chamadas React devem apontar para o endereço correto e respeitar as regras CORS/cookies.
4. Confirma que o PostgreSQL está `healthy` com `docker compose ps`.

### Erro `404` em `/api/health`

A rota implementada é **`GET /health`**, não `/api/health`. Abre `http://localhost:5185/health`.

### `Invoke-RestMethod`: `Unable to connect to the remote server`

O PowerShell não consegue ligar à API; o DBeaver pode estar conectado ao PostgreSQL e, mesmo assim, a API não estar em execução. Num terminal separado, na raiz, executa:

```powershell
dotnet run --project backend/PersonalFinance.Api --launch-profile http
```

Mantém a API aberta e tenta `http://localhost:5185/health`. Se estiveres noutra porta, passa `-BaseUrl` ao script de teste.

### `test-transactions.ps1` não é reconhecido pelo PowerShell

Se estiveres na pasta `scripts`, utiliza `.\test-transactions.ps1` e não apenas `test-transactions.ps1`. Se estiveres na raiz do projeto, utiliza `.\scripts\test-transactions.ps1`.

### O Swagger devolve `401 Unauthorized`

Primeiro usa `POST /auth/login` no próprio Swagger e confirma `200`. Depois executa o endpoint protegido na mesma aba/navegador, para que o cookie `pf.auth` seja enviado. Sem sessão válida, `401` é o comportamento esperado.

### `POST /api/transactions` devolve `404` para conta ou categoria

Confirma que os GUIDs existem e pertencem ao utilizador autenticado. Utiliza `GET /api/accounts` e, apenas em Development, `GET /dev/users/{userId}/categories`. Categoria `Expense` com transação `Income` (ou vice-versa) causa `400`.

### O teste de contas falha em `/dev/auth/login`

O script `scripts/test-accounts.ps1` contém uma rota antiga. Atualiza as duas ocorrências para `/auth/login`. O endpoint de login atual é `POST /auth/login`.

### `dotnet tool restore` não encontra o manifesto

No projeto analisado, o manifesto de ferramentas chama-se `dotnet-tools.json` e fica na raiz. Executa `dotnet tool restore --tool-manifest ./dotnet-tools.json`.

### `GET /health` devolve `503`

A API arrancou, mas não conseguiu aceder ao PostgreSQL. Verifica `docker compose ps`, `.env`, User Secrets, porta e credenciais efetivas da base de dados.

### `git clone` pede acesso ou devolve `Repository not found`

O endereço pode estar incorreto ou o repositório pode ser privado. Pede ao responsável pelo GitHub o **URL real do repositório** e acesso à equipa. Não uses o link do GitHub Project para clonar código.

---

## 9. Funcionalidades planeadas

Prioridades do projeto (MoSCoW):

| Prioridade | Funcionalidades |
| --- | --- |
| **MUST** | Registo, login e perfil; recuperação de palavra-passe; contas; categorias; receitas e despesas com filtros; orçamentos mensais; dashboard; metas de poupança. |
| **SHOULD** | Alertas visuais de orçamento e relatórios. |
| **COULD** | Reconhecimento experimental de faturas, simulador de gastos e sugestões simples baseadas em regras. |
| **WON'T (nesta versão)** | Aplicação móvel, integração bancária e modelo de IA de gestão financeira. |

**Ordem sugerida:** setup → autenticação/contas → categorias/transações → orçamentos/metas/dashboard → alertas/relatórios → testes e apresentação.

### Funcionalidades já existentes versus trabalho futuro

| Área | Estado observado no código analisado |
| --- | --- |
| Registo, login, perfil e logout | **Implementado no backend** com ASP.NET Core Identity e cookie `pf.auth`. |
| CRUD de contas | **Implementado no backend**, com controlo por utilizador. |
| CRUD de transações | **Implementado no backend**, com filtros e validações. |
| CRUD de orçamentos e alertas | **Implementado no backend**, incluindo alertas a partir de 80%. |
| Categorias | Modelo, tabelas e dados de demonstração existentes; **sem CRUD público** nesta versão. |
| Dashboard | Resumo disponível em **`/dev`**, sem endpoint público de dashboard nesta versão. |
| Metas de poupança | Modelo e dados de demonstração existentes; **sem CRUD público** nesta versão. |
| Recuperação de password | **Planeada**, não identificada no `AuthController` atual. |
| Interface React funcional | **Por desenvolver**; o template inicial do Vite está presente. |
| Swagger e testes xUnit | **Configurados no backend**, com testes para vários módulos. |

Esta tabela descreve as funcionalidades **presentes no código** disponibilizado, não certifica que estejam todas compiladas ou testadas com sucesso na tua máquina. A existência de um modelo na base de dados não implica que exista uma página React ou API pública correspondente.

---

## 10. Como criar a estrutura inicial (apenas uma vez)

> **Esta secção destina-se a quem está a criar/configurar o repositório pela primeira vez.** Se clonaste um repositório que já contém `backend/` e `frontend/`, **não voltes a executar estes comandos**. Podes sobrescrever ou duplicar trabalho existente.

Na pasta raiz de um repositório **novo e vazio**, para criar o backend:

```powershell
dotnet new sln -n PersonalFinance

dotnet new webapi -n PersonalFinance.Api -o backend/PersonalFinance.Api --use-controllers --framework net10.0

dotnet new xunit -n PersonalFinance.Tests -o backend/PersonalFinance.Tests --framework net10.0

dotnet sln add backend/PersonalFinance.Api/PersonalFinance.Api.csproj

dotnet sln add backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj

dotnet add backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj reference backend/PersonalFinance.Api/PersonalFinance.Api.csproj
```

Para criar o frontend (a primeira vez que aparece o assistente, **escolhe ESLint** como linter):

```powershell
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
cd ..
```

**No repositório atual, não voltes a instalar estes pacotes:** o `.csproj` já contém as referências de persistência, Identity e Swagger. A base do projeto usa .NET 10, com **EF Core/Npgsql/Identity 8.x**, e o gestor de pacotes deve respeitar as versões efetivamente definidas nos `.csproj`. Para um clone existente, usa `dotnet restore PersonalFinance.slnx`; não mistures indiscriminadamente EF Core 10 com as dependências EF Core 8 do projeto.

Num projeto criado do zero também seria necessário configurar `docker-compose.yml`, `.env.example`, `.gitignore`, `Program.cs` e o `AppDbContext`. **Estes ficheiros e a migration inicial já existem neste repositório.**

O projeto já possui o manifesto de ferramentas local **`dotnet-tools.json`** (na raiz), com `dotnet-ef` **8.0.33**. Para o restaurar, executa:

```powershell
dotnet tool restore --tool-manifest ./dotnet-tools.json
```

**Não** executes `dotnet new tool-manifest` num clone já configurado.

A migration **`InitialCreate` já está criada e versionada**. Quem clonar o projeto só precisa de a aplicar:

```powershell
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

Para uma estrutura criada do zero, a primeira migration teria de ser gerada **uma única vez** após criar os modelos e o `AppDbContext`; isso não faz parte do setup normal da equipa.

Depois do setup, o responsável deve testar os comandos da [secção 4](#4-preparar-e-executar-o-projeto-pela-primeira-vez) como se fosse um novo membro da equipa e enviar os ficheiros para o GitHub.

---

## 11. Checklist de instalação e execução

Antes de começar a desenvolver ou demonstrar os CRUDs, confirma:

- [ ] `.NET SDK 10`, Node.js, npm, Git e Docker instalados.
- [ ] Ficheiro `.env` criado a partir de `.env.example` e não enviado ao Git.
- [ ] Docker Desktop iniciado e `docker compose ps` indica PostgreSQL `healthy`.
- [ ] `ConnectionStrings:DefaultConnection` configurada em User Secrets com as credenciais corretas.
- [ ] `dotnet restore PersonalFinance.slnx` e ferramentas locais restauradas.
- [ ] Migrations aplicadas com `dotnet ef database update`.
- [ ] Seed executado **se** precisares de dados de demonstração.
- [ ] API iniciada em `http://localhost:5185` e `GET /health` devolve `database: connected`.
- [ ] Swagger disponível em `http://localhost:5185/swagger` no ambiente Development.
- [ ] Login em `POST /auth/login` efetuado antes de testar endpoints protegidos.
- [ ] IDs reais de contas e categorias obtidos antes de criar transações ou orçamentos.
- [ ] `dotnet test backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj` executado e resultados verificados.
- [ ] Script `test-transactions.ps1` executado contra a API local, se precisares do teste HTTP completo.
- [ ] Rota antiga corrigida no script de contas antes de o executares.
- [ ] `git status` confirmado, sem segredos ou ficheiros gerados incluídos nos commits.

> **Nota final:** este README documenta a versão do código inspecionada em novembro de 2026. Se novas branches forem integradas, atualiza as secções de funcionalidades e endpoints em simultâneo com o código.
