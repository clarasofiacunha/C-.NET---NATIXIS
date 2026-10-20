# Personal Finance App

Aplicação web para **gestão de finanças pessoais**, desenvolvida em equipa. O objetivo é permitir que cada utilizador acompanhe contas, receitas, despesas, orçamentos mensais e metas de poupança num único local.

> **Estado do projeto:** a estrutura abaixo é a estrutura **prevista** para o desenvolvimento. Alguns ficheiros e funcionalidades poderão ainda não existir, conforme a fase em que a equipa se encontra. A secção [Estrutura e responsabilidades das pastas](#5-estrutura-e-responsabilidades-das-pastas) distingue o setup inicial do trabalho futuro.

## Índice

1. [O que é a aplicação?](#1-o-que-é-a-aplicação)
2. [Tecnologias utilizadas](#2-tecnologias-utilizadas)
3. [Instalar as ferramentas necessárias](#3-instalar-as-ferramentas-necessárias)
4. [Preparar e executar o projeto pela primeira vez](#4-preparar-e-executar-o-projeto-pela-primeira-vez)
5. [Estrutura e responsabilidades das pastas](#5-estrutura-e-responsabilidades-das-pastas)
6. [Como trabalhar no projeto](#6-como-trabalhar-no-projeto)
7. [Testar e validar a instalação](#7-testar-e-validar-a-instalação)
8. [Problemas frequentes e soluções](#8-problemas-frequentes-e-soluções)
9. [Funcionalidades planeadas](#9-funcionalidades-planeadas)
10. [Como criar a estrutura inicial (apenas uma vez)](#10-como-criar-a-estrutura-inicial-apenas-uma-vez)
11. [Checklist de instalação](#11-checklist-de-instalação)

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
Navegador (React)
    |
    | Pedido HTTP /api/...
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
| **Entity Framework Core** | Ler, gravar e atualizar dados | **EF Core 10** |
| **PostgreSQL** | Guardar dados | **PostgreSQL 17** via Docker |
| **Docker Desktop / Compose** | Executar a base de dados localmente | Versão estável |
| **Git e GitHub** | Partilhar e versionar código | Versão estável |
| **xUnit** | Executar testes do backend | A definida no projeto de testes |

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
4. Executa (substitui o URL de exemplo pelo URL **real**):

```powershell
git clone https://github.com/UTILIZADOR/personal-finance-app.git
cd personal-finance-app
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
POSTGRES_PASSWORD=escolhe_uma_password_local
```

Altera `POSTGRES_PASSWORD` para uma password tua, usada apenas no desenvolvimento local. **Não partilhes o teu `.env` nem o publiques no GitHub.**

Se não existe `.env.example` ou `docker-compose.yml`, o setup do repositório ainda não está concluído: pede ao responsável pelo setup para adicionar esses ficheiros (ver [secção 10](#10-como-criar-a-estrutura-inicial-apenas-uma-vez)).

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
dotnet tool restore
```

O segundo comando instala as ferramentas .NET locais definidas no ficheiro `.config/dotnet-tools.json`, incluindo `dotnet-ef` **se a equipa já o tiver adicionado**.

**Configurar a ligação à base de dados sem guardar passwords no código:**

```powershell
dotnet user-secrets init --project backend/PersonalFinance.Api
```

Depois, executa numa **só linha**, substituindo `A_TUA_PASSWORD` pelo valor utilizado em `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Host=localhost;Port=5432;Database=personal_finance;Username=personal_finance_user;Password=A_TUA_PASSWORD' --project backend/PersonalFinance.Api
```

Usamos **User Secrets** para a password não aparecer no código versionado. Cada colega deve configurar os seus próprios segredos na respetiva máquina.

**Aplicar as migrations da base de dados:**

```powershell
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

Este comando exige que a equipa já tenha criado e versionado uma migration inicial na pasta `Migrations/`. **Não cries uma nova migration `InitialCreate` em cada computador.** Se ainda não existir nenhuma migration, consulta a [secção 10](#10-como-criar-a-estrutura-inicial-apenas-uma-vez) ou contacta o responsável pelo backend.

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

`npm ci` utiliza o ficheiro `package-lock.json` versionado e instala as dependências nas versões combinadas pela equipa. Se o frontend ainda não tiver `package-lock.json`, o responsável pelo setup deve executar `npm install` e versionar esse ficheiro.

### Passo 6 — Iniciar a aplicação (três terminais)

**Terminal 1 — Base de dados** (na raiz):

```powershell
docker compose up -d
```

**Terminal 2 — Backend** (na raiz):

```powershell
dotnet run --project backend/PersonalFinance.Api -- --urls http://localhost:5000
```

**Terminal 3 — Frontend**:

```powershell
cd frontend
npm run dev
```

Deixa os terminais do backend e frontend abertos enquanto estiveres a usar a aplicação. Para parar um destes servidores, usa `Ctrl + C` no respetivo terminal.

### Passo 7 — Abrir e confirmar

- **Frontend:** <http://localhost:5173>
- **Teste de ligação da API à base de dados:** <http://localhost:5000/api/health>

Quando o endpoint `/api/health` já estiver implementado, a resposta esperada é:

```json
{
  "status": "ok",
  "database": "connected"
}
```

Se o React já tiver o teste de integração descrito no setup inicial, também poderá apresentar `System status: ok - connected`.

> **Nota:** estas são verificações **esperadas quando a equipa tiver implementado esses ficheiros/endpoints**. O README não significa que a autenticação, dashboard ou outras páginas já estejam disponíveis.

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

A estrutura abaixo mostra o destino pretendido para o projeto. **Nem todos os ficheiros são gerados automaticamente**, e alguns surgirão apenas quando forem implementadas as respetivas funcionalidades.

```text
personal-finance-app/                  # PASTA PRINCIPAL / RAIZ
│
├── backend/                           # Código C# e testes
│   ├── PersonalFinance.Api/           # Projeto ASP.NET Core Web API
│   │   ├── Controllers/               # Endpoints HTTP (podem surgir por módulo)
│   │   │   ├── AuthController.cs       # [FUTURO] registo/login/perfil (se usado)
│   │   │   ├── AccountsController.cs   # [FUTURO] gestão de contas
│   │   │   ├── TransactionsController.cs # [FUTURO] receitas/despesas
│   │   │   └── ...
│   │   ├── Services/                  # [FUTURO] regras de negócio
│   │   ├── DTOs/                      # [FUTURO] formatos de entrada/saída da API
│   │   ├── Models/                    # Entidades, por exemplo Account.cs
│   │   ├── Data/                      # AppDbContext e configurações EF Core
│   │   ├── Migrations/                # Criada pelo EF Core na primeira migration
│   │   ├── Program.cs                 # Arranque/configuração da API
│   │   ├── appsettings.json           # Configuração não secreta
│   │   └── PersonalFinance.Api.csproj # Dependências do backend
│   │
│   └── PersonalFinance.Tests/         # Testes automatizados em xUnit
│       └── PersonalFinance.Tests.csproj
│
├── frontend/                          # Interface React + TypeScript
│   ├── src/
│   │   ├── components/                # [FUTURO] componentes reutilizáveis
│   │   ├── pages/                     # [FUTURO] páginas: login, dashboard...
│   │   ├── features/                  # [FUTURO] organização por funcionalidades
│   │   ├── services/                  # [FUTURO] chamadas à API
│   │   ├── hooks/                     # [FUTURO] hooks React personalizados
│   │   ├── types/                     # [FUTURO] interfaces/tipos TypeScript
│   │   ├── App.tsx                    # Componente principal
│   │   └── main.tsx                   # Entrada da aplicação React
│   ├── public/                        # Ficheiros públicos estáticos
│   ├── package.json                   # Dependências/comandos npm
│   ├── package-lock.json              # Versões exatas das dependências
│   └── vite.config.ts                 # Configuração Vite/proxy para a API
│
├── docs/                              # [FUTURO] diagramas e documentação
├── .github/
│   └── workflows/
│       └── ci.yml                     # [QUANDO CONFIGURADO] testes automáticos
├── .config/
│   └── dotnet-tools.json              # [QUANDO CONFIGURADO] ferramentas .NET locais
├── docker-compose.yml                # PostgreSQL em Docker
├── .env.example                      # Exemplo SEM passwords reais
├── .env                              # Configuração local (NÃO enviar ao Git)
├── .gitignore                        # Ficheiros a não enviar para Git
├── PersonalFinance.slnx              # Solução .NET (SDK 10)
└── README.md                          # Este guia
```

### Backend: o que vai em cada pasta?

| Pasta ou ficheiro | O que é? | Exemplo |
| --- | --- | --- |
| `Controllers/` | Recebe os pedidos do React e devolve respostas HTTP. | `GET /api/accounts` |
| `Services/` | Contém cálculos e regras financeiras. | Calcular o orçamento já utilizado. |
| `DTOs/` | Define os dados que a API aceita e devolve. | `CreateTransactionDto` |
| `Models/` | Representa os objetos do negócio. | `Account`, `Transaction`, `Budget` |
| `Data/` | Configura o Entity Framework e a ligação aos dados. | `AppDbContext` |
| `Migrations/` | Guarda o histórico de alterações à estrutura da base de dados. | `InitialCreate` |
| `Program.cs` | Regista serviços e configura as rotas/middleware. | API, DbContext, OpenAPI |
| `PersonalFinance.Tests/` | Contém testes do comportamento esperado. | Testar cálculo de saldo. |

**Regras importantes:**

- Os controllers devem ser leves: operações complexas pertencem aos serviços.
- Os DTOs impedem que a interface dependa diretamente de todas as propriedades dos modelos internos.
- Cada operação sobre contas/transações deverá verificar se os dados pertencem ao utilizador autenticado.
- Valores monetários em C# devem usar `decimal` (e no PostgreSQL, um tipo decimal apropriado).
- **Não guardes passwords nem tokens no código ou no Git.**

### Frontend: o que vai em cada pasta?

| Pasta ou ficheiro | O que é? | Exemplo |
| --- | --- | --- |
| `components/` | Peças reutilizáveis da interface. | Botões, inputs, cartões. |
| `pages/` | Ecrãs inteiros. | Login, dashboard, transações. |
| `features/` | Código agrupado por módulo funcional. | `transactions/`, `budgets/` |
| `services/` | Funções que fazem pedidos à API. | `accountsApi.ts` |
| `hooks/` | Lógica React reutilizável. | `useAccounts.ts` |
| `types/` | Definições de tipos e interfaces. | `Transaction.ts` |
| `App.tsx` | Organiza a aplicação/rotas principais. | Layout geral. |
| `vite.config.ts` | Configura a execução do frontend. | Proxy `/api` → `localhost:5000`. |

As pastas `Services/`, `DTOs/`, `features/` e outras podem estar **vazias no início**. Isso é normal: receberão ficheiros quando as funcionalidades forem desenvolvidas. Por padrão, o Git não guarda pastas vazias; podem usar um `.gitkeep` se precisarem de as mostrar no repositório.

### Ficheiros na raiz: por que existem?

| Ficheiro | Função |
| --- | --- |
| `README.md` | Ensina a preparar, executar e compreender o projeto. |
| `PersonalFinance.slnx` | Agrupa os projetos de backend/testes. |
| `docker-compose.yml` | Descreve o contentor de PostgreSQL. |
| `.env.example` | Indica as variáveis necessárias, com valores de exemplo. |
| `.env` | Contém configurações **locais e privadas**; não deve ir para o Git. |
| `.gitignore` | Impede o envio de passwords, `node_modules`, `bin/obj` etc. |
| `.github/workflows/ci.yml` | Executa validações automaticamente no GitHub quando configurado. |

---

## 6. Como trabalhar no projeto

### 6.1. Começar a trabalhar

Antes de desenvolver, obtém as alterações mais recentes da equipa. Por exemplo, se estiverem a usar `develop` como branch de integração:

```powershell
git switch develop
git pull origin develop
```

Para trabalhar numa nova funcionalidade, cria uma branch própria:

```powershell
git switch -c feature/accounts
```

Faz as alterações, executa os testes e envia o trabalho:

```powershell
git add .
git commit -m "feat: add accounts management"
git push -u origin feature/accounts
```

No GitHub, abre um **Pull Request** de `feature/accounts` para `develop` e pede revisão a outro elemento.

> Se o repositório ainda não utilizar `develop`, combina primeiro a estratégia de branches com a equipa. Não executes comandos que substituam trabalho existente.

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

## 7. Testar e validar a instalação

Executa estes comandos **na raiz do projeto**, exceto quando indicado:

**Backend — compilar:**

```powershell
dotnet build PersonalFinance.slnx
```

**Backend — testes unitários:**

```powershell
dotnet test PersonalFinance.slnx
```

**Frontend — analisar o código com ESLint:**

```powershell
cd frontend
npm run lint
```

**Frontend — criar o build:**

```powershell
npm run build
cd ..
```

**Base de dados — verificar o contentor:**

```powershell
docker compose ps
```

**Integração — se já existir o endpoint de health:**

Abre <http://localhost:5000/api/health> enquanto a API está a executar. A resposta esperada é `status: ok` e `database: connected`.

Se o `npm run lint` não existir, confirma que o frontend foi criado com ESLint e que o respetivo script está definido em `frontend/package.json`.

A pipeline `.github/workflows/ci.yml`, quando adicionada, deverá automatizar pelo menos a compilação e os testes.

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

### `dotnet` não é reconhecido / versão errada

Instala o **.NET SDK 10**, fecha e reabre o terminal e confirma `dotnet --version`.

### `dotnet ef` não é reconhecido

Na raiz, executa `dotnet tool restore`. Se não existir `.config/dotnet-tools.json`, o responsável pelo setup deve adicionar e versionar a ferramenta.

### `No migrations were found` ou pasta `Migrations/` em falta

Ainda poderá não existir uma migration versionada. **Não cries migrations independentes em cada computador.** A primeira deve ser criada uma vez por um responsável e enviada para o GitHub.

### `npm` não é reconhecido

Instala Node.js LTS e reabre o PowerShell. Verifica `node --version` e `npm --version`.

### `npm ci` falha por falta de `package-lock.json`

O responsável pelo frontend deverá executar `npm install` na pasta `frontend/` e enviar o `package-lock.json` para o repositório. Depois, os restantes elementos usam `npm ci`.

### O React abre, mas a API não responde

1. Confirma que a API está a correr em `http://localhost:5000`.
2. Confirma que `frontend/vite.config.ts` encaminha `/api` para `http://localhost:5000`.
3. Verifica se `/api/health` já está implementado no backend.
4. Confirma que o PostgreSQL está `healthy`.

### Erro `404` em `/api/health`

A API pode estar a funcionar, mas o endpoint de teste ainda não foi implementado. Confirma o `Program.cs` e o estado atual do setup antes de assumir que a ligação à base de dados falhou.

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

O primeiro marco funcional deverá permitir que um utilizador **se registe, inicie sessão e crie uma conta financeira associada ao seu perfil**. Depois, a equipa desenvolve as transações e os restantes módulos.

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

Para instalar os pacotes de persistência no backend, utiliza **versões compatíveis com o .NET SDK 10** e o EF Core 10 definidas pela equipa:

```powershell
dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3

dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.12

dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.12
```

Também deve ser criada e enviada para o GitHub a configuração `docker-compose.yml`, o `.env.example`, o `.gitignore`, o `Program.cs` com a ligação EF Core e, quando estiver pronto, o `AppDbContext` e a primeira migration.

A ferramenta `dotnet-ef` deve ser registada e versionada **uma única vez** na raiz:

```powershell
dotnet new tool-manifest
dotnet tool install --local dotnet-ef --version 10.0.12
```

Depois de criar as entidades e o `AppDbContext`, criar a **primeira migration** uma única vez e enviar os ficheiros da pasta `Migrations/` para o Git:

```powershell
dotnet ef migrations add InitialCreate --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api

dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

Estas migrations não podem ser geradas antes de a equipa implementar as classes e configurações correspondentes. Os restantes membros executam **apenas** `dotnet ef database update`.

Depois do setup, o responsável deve testar os comandos da [secção 4](#4-preparar-e-executar-o-projeto-pela-primeira-vez) como se fosse um novo membro da equipa e enviar os ficheiros para o GitHub.

---
