# Personal Finance App — Resumo da conversa e guia de setup inicial

> **Objetivo:** consolidar as decisões técnicas tomadas na conversa, explicar os conceitos por trás delas e documentar como preparar uma máquina Windows para desenvolver a aplicação. Os comandos pressupõem PowerShell, .NET 10, Node.js 24 e que o projeto tem o nome `personal-finance-app`.
>


## 1. Contexto e âmbito do projeto

O **Personal Finance App** é uma aplicação web de gestão de finanças pessoais, desenvolvida por uma equipa de quatro pessoas, com aproximadamente seis semanas para uma primeira apresentação. Não é uma aplicação bancária: as contas e as transações são registadas e geridas dentro da própria aplicação.

As prioridades definidas foram:

| MoSCoW | Funcionalidades |
|---|---|
| **MUST** | Registo/login/perfil, recuperação de palavra-passe, contas financeiras, categorias, receitas e despesas (CRUD e filtros), orçamentos mensais, dashboard e metas de poupança. |
| **SHOULD** | Alertas visuais para orçamentos e relatórios. |
| **COULD** | Simulador de gastos, sugestões simples por regras e reconhecimento experimental de faturas. |
| **WON'T (nesta versão)** | Aplicação móvel, ligação a contas bancárias e modelo de IA para gestão financeira. |


## 2. Arquitetura e escolhas técnicas

### 2.1 Monólito modular com API REST

Foi recomendado um **monólito modular**: uma aplicação backend central, organizada por responsabilidades e funcionalidades, em vez de diversos microserviços.


Fluxo principal:

```text
Browser / React
       |
       | HTTP / JSON (API REST)
       v
ASP.NET Core (Controllers -> Services -> Data)
       |
       | Entity Framework Core / Npgsql
       v
PostgreSQL (Docker)
```

O frontend **não se liga diretamente** à base de dados: a API valida pedidos, autentica utilizadores, aplica regras de negócio e controla acesso aos dados.

### 2.2 Tecnologias e razões

| Tecnologia | Papel | Razão da escolha |
|---|---|---|
| **C# / ASP.NET Core 10** | Backend e API REST | Ecossistema sólido, tipagem forte, integração com Identity e Entity Framework Core. |
| **React + TypeScript** | Interface do utilizador | Componentes reutilizáveis; TypeScript deteta erros de tipos durante o desenvolvimento. |
| **Vite** | Ambiente de desenvolvimento/frontend build | Setup rápido, servidor de desenvolvimento eficiente e build para produção. |
| **ESLint** | Análise estática de código frontend | Regras consolidadas e integração com o VS Code; foi escolhido em vez de Oxlint durante a criação do frontend. |
| **PostgreSQL 17** | Base de dados relacional | Transações, integridade referencial e boa adequação a dados financeiros estruturados. |
| **Entity Framework Core 10 + Npgsql** | ORM e ligação a PostgreSQL | Permite mapear classes C# para tabelas e versionar alterações através de migrations. |
| **ASP.NET Core Identity** | Gestão de utilizadores | Evita construir autenticação e armazenamento de passwords de raiz. |
| **Docker Compose** | Executar PostgreSQL localmente | Ambiente semelhante para toda a equipa, fácil de iniciar/parar. |
| **xUnit** | Testes backend | Verificação automática de lógica e regressões. |
| **Git + GitHub + GitHub Actions** | Histórico, colaboração e CI | Trabalho com branches, revisão por Pull Requests e validação automática. |

**REST:** estilo de API que usa recursos e métodos HTTP (`GET`, `POST`, `PUT`/`PATCH`, `DELETE`). Em geral, pedidos/respostas transportam JSON.

**ORM:** *Object-Relational Mapper*, camada que relaciona classes/objetos com dados em tabelas. Não elimina a necessidade de compreender relações, chaves e SQL.

**Migration:** descrição versionada de uma alteração ao esquema da base de dados. Deve ser criada uma vez pela equipa, colocada no Git e aplicada por cada pessoa na sua base local.

**Linter:** ferramenta que identifica problemas e violações de regras de código antes da execução. **ESLint** foi recomendado para o projeto por ser consolidado; o **Oxlint** é uma alternativa válida, com foco na velocidade.

### 2.3 Convenções de nomes

| Item | Nome adotado |
|---|---|
| Repositório/pasta raiz | `personal-finance-app` |
| Solution .NET | `PersonalFinance.slnx` |
| API | `PersonalFinance.Api` |
| Testes | `PersonalFinance.Tests` |
| Frontend | `frontend` |
| Base de dados | `personal_finance` |
| Utilizador PostgreSQL de exemplo | `personal_finance_user` |
| Contentor Docker | `personal-finance-db` |

O nome anterior **FÔLEGO** foi abandonado nas instruções de setup. Os nomes de código e a documentação devem ser consistentes.

## 3. Pré-requisitos no Windows

Instalar: **Git**, **.NET SDK 10**, **Node.js 24 LTS** (inclui npm), **Docker Desktop**, **Visual Studio Code**. Postman e DBeaver são opcionais para testar APIs e explorar a base de dados.

Verificar no PowerShell:

```powershell
git --version
dotnet --version
node --version
npm --version
docker --version
docker compose version
```

### Erro de Docker Desktop encontrado na conversa

O instalador mostrou: **`Virtual Machine Platform` is not enabled**. O Docker Desktop em Windows usa frequentemente WSL 2 para contentores Linux, e a funcionalidade de virtualização necessária ainda não estava ativa.

1. Fechar o instalador.
2. Abrir **PowerShell como administrador**.
3. Executar:

   ```powershell
   wsl --install
   ```

4. Caso a funcionalidade continue inativa, executar:

   ```powershell
   dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
   ```

5. **Reiniciar** o Windows.
6. Verificar:

   ```powershell
   wsl --status
   wsl --set-default-version 2
   ```

7. Reinstalar/abrir Docker Desktop, confirmar que utiliza o motor WSL 2 e testar:

   ```powershell
   docker run --rm hello-world
   ```

**Teoria:** WSL 2 disponibiliza um kernel Linux num ambiente virtualizado. O Docker Engine consegue assim executar contentores Linux. Num computador de empresa ou escola, pode ser necessária autorização administrativa; se a virtualização estiver indisponível no firmware/BIOS, os comandos Windows poderão não bastar.

## 4. Criar ou obter o código

**Importante:** `https://github.com/users/clarasofiacunha/projects/6` é um **GitHub Project** (quadro de planeamento), não um endereço de clone. O repositório de código é outro endereço, obtido pelo botão **Code** do repositório.

Se o código já existir, **clonar e seguir para o passo 5**, sem gerar outra vez a solution ou o frontend:

```powershell
git clone https://github.com/UTILIZADOR_OU_ORGANIZACAO/personal-finance-app.git
cd personal-finance-app
```

O endereço acima é apenas um modelo: usar o URL real fornecido pelo GitHub.

### 4.1 Só para quem cria o repositório pela primeira vez

Na raiz do repositório vazio:

```powershell
dotnet new sln -n PersonalFinance
dotnet new webapi -n PersonalFinance.Api -o backend/PersonalFinance.Api --use-controllers --framework net10.0
dotnet new xunit -n PersonalFinance.Tests -o backend/PersonalFinance.Tests --framework net10.0
dotnet sln add backend/PersonalFinance.Api/PersonalFinance.Api.csproj
dotnet sln add backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj
dotnet add backend/PersonalFinance.Tests/PersonalFinance.Tests.csproj reference backend/PersonalFinance.Api/PersonalFinance.Api.csproj
dotnet build
dotnet test
```

**Solution:** ficheiro que agrupa os projetos .NET. **Projeto API:** executável web. **Projeto de testes:** código separado que valida o comportamento da aplicação. Em .NET 10, `dotnet new sln` cria, por predefinição, `.slnx`.

Criar frontend:

```powershell
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
npm run dev
```

Durante a criação foi perguntado **“Which linter to use?”**; foi recomendado **ESLint**. Parar o servidor com `Ctrl+C` e regressar à raiz com `cd ..`.

**Não voltar a executar `npm create vite` quando o `frontend/` já está no repositório.** Numa máquina nova, entrar em `frontend/` e executar `npm ci` para instalar as dependências do `package-lock.json`.

### 4.2 Estrutura e responsabilidades das pastas

```text
personal-finance-app/
├── backend/
│   ├── PersonalFinance.Api/
│   │   ├── Controllers/      # Entradas HTTP e respostas
│   │   ├── Services/         # Regras de negócio
│   │   ├── DTOs/             # Objetos para pedidos/respostas da API
│   │   ├── Models/           # Entidades do domínio
│   │   ├── Data/             # DbContext e acesso à persistência
│   │   ├── Migrations/       # Alterações versionadas do esquema
│   │   └── Program.cs       # Configuração e arranque da API
│   └── PersonalFinance.Tests/  # Testes automatizados
├── frontend/
│   ├── src/
│   │   ├── components/      # Elementos visuais reutilizáveis
│   │   ├── pages/           # Ecrãs/rotas
│   │   ├── features/        # Código organizado por funcionalidade
│   │   ├── services/        # Pedidos HTTP à API
│   │   ├── hooks/           # Hooks React personalizados
│   │   └── types/           # Interfaces e tipos TypeScript
│   ├── package.json
│   └── vite.config.ts
├── docs/                   # Documentação funcional/técnica
├── .github/workflows/      # Pipeline de CI
├── docker-compose.yml      # Serviços Docker
├── .env.example            # Modelo de configuração sem segredos reais
├── .gitignore              # Ficheiros que não entram no Git
├── PersonalFinance.slnx
└── README.md
```

Algumas pastas são **planeadas** e só aparecem quando criadas; por exemplo, `Migrations/` surge após a primeira migration. O Git não guarda pastas vazias por si só.

**Controller vs Service vs DTO:** o controller recebe o pedido HTTP, o service aplica as regras do negócio e o DTO define exatamente os dados que entram/saem da API. Esta separação facilita testes, manutenção e impede expor indiscriminadamente os modelos de base de dados.

## 5. PostgreSQL com Docker Compose

### 5.1 Configurar os ficheiros

Criar `docker-compose.yml` na raiz:

```yaml
services:
  db:
    image: postgres:17
    container_name: personal-finance-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "127.0.0.1:5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB"]
      interval: 5s
      timeout: 5s
      retries: 10
volumes:
  postgres_data:
```

Criar `.env.example`:

```dotenv
POSTGRES_DB=personal_finance
POSTGRES_USER=personal_finance_user
POSTGRES_PASSWORD=change_this_password
```

Cada pessoa cria o seu `.env` a partir do modelo:

```powershell
Copy-Item .env.example .env
```

Editar a password no `.env`. **Não enviar `.env` para o GitHub.** O `.env.example` deve conter apenas valores demonstrativos.

**Teoria:** Docker *image* é o modelo usado para criar o contentor; *container* é a instância em execução; *volume* conserva os dados mesmo quando o contentor é parado/removido. O Compose descreve declarativamente como iniciar o serviço. `127.0.0.1:5432:5432` limita a publicação da porta à própria máquina.

### 5.2 Iniciar e verificar

```powershell
docker compose up -d
docker compose ps
docker compose logs db
```

Deve aparecer `personal-finance-db` em execução e, após alguns segundos, `healthy`.

Para parar mantendo os dados:

```powershell
docker compose down
```

`docker compose down -v` remove também o volume e **pode destruir dados locais**. Utilizá-lo apenas quando se pretende reinicializar deliberadamente o ambiente.

**Atenção a credenciais antigas:** `POSTGRES_USER`, `POSTGRES_DB` e `POSTGRES_PASSWORD` só fazem a inicialização automática quando o diretório de dados está vazio. Se o volume já existir, mudar o `.env` não recria automaticamente o utilizador ou a base de dados. Se surgir `role ... does not exist`, confirmar qual o utilizador efetivamente existente e se o volume é antigo; não apagar o volume sem avaliar os dados que contém.

## 6. Entity Framework Core e ASP.NET Core Identity

### 6.1 Instalar os pacotes da API

Os comandos discutidos foram:

```powershell
dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet add backend/PersonalFinance.Api/PersonalFinance.Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.12
```

**Se o projeto já os tiver no `.csproj`, não os reinstalar por rotina.** As versões acima representam a configuração proposta durante a conversa e podem ser atualizadas pela equipa de forma coordenada.

### 6.2 Entidade inicial e contexto

`Models/Account.cs`:

```csharp
namespace PersonalFinance.Api.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
}
```

`Data/AppDbContext.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Models;

namespace PersonalFinance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(100).IsRequired();
            entity.Property(a => a.OpeningBalance).HasPrecision(18, 2);
            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

**Teoria:** `DbContext` representa a sessão de acesso aos dados; `DbSet<Account>` corresponde às contas. `UserId` é a chave estrangeira do utilizador, gerido pelo Identity. `decimal` e precisão (`18,2`) são adequados a valores monetários. A relação por chave estrangeira garante integridade, **mas não garante por si só autorização**: os endpoints terão sempre de filtrar as contas pelo utilizador autenticado.

### 6.3 Guardar a connection string fora do código

```powershell
dotnet user-secrets init --project backend/PersonalFinance.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=personal_finance;Username=personal_finance_user;Password=A_TUA_PASSWORD" --project backend/PersonalFinance.Api
```

Trocar `A_TUA_PASSWORD` pela password definida no `.env`. Estes **User Secrets** são adequados para desenvolvimento, não para armazenamento seguro de segredos de produção.

**Porquê `.env` e User Secrets?** O `.env` fornece variáveis ao Docker Compose; os User Secrets fornecem a connection string ao processo .NET. Os dois ambientes consomem a mesma password local, mas são configurados separadamente.

### 6.4 Program.cs e teste de conectividade

O backend foi configurado para adicionar `Controllers`, `OpenAPI` e `AppDbContext` com `UseNpgsql`, e para disponibilizar um endpoint temporário de teste:

```csharp
app.MapGet("/api/health", async (AppDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();
    return connected
        ? Results.Ok(new { status = "ok", database = "connected" })
        : Results.Problem("Database connection failed.");
});
```

No `Program.cs`, é necessário **registar o DbContext antes de construir a aplicação**:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
```

Além disso, configurar `builder.Services.AddControllers()`, `builder.Services.AddOpenApi()` e `app.MapControllers()`; disponibilizar a especificação OpenAPI apenas no ambiente de desenvolvimento, conforme o template criado. O endpoint de saúde não substitui testes completos de autenticação e regras financeiras.

### 6.5 Gerar e aplicar a primeira migration

**Apenas quem prepara a base do projeto cria a migration inicial**:

```powershell
dotnet new tool-manifest
dotnet tool install --local dotnet-ef --version 10.0.12
dotnet ef migrations add InitialCreate --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

**Restantes pessoas, após clonar:**

```powershell
dotnet tool restore
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

As migrations criam as tabelas do Identity e a `Accounts`, de acordo com os modelos registados nesse momento. A implementação de login, permissões e recuperação da password continua a ser uma etapa futura.

## 7. React a comunicar com a API

No `frontend/vite.config.ts`, configurar proxy de desenvolvimento:

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
```

**Teoria:** *proxy* é um intermediário. No desenvolvimento, um pedido do browser para `http://localhost:5173/api/health` é reenviado pelo Vite para a API em `http://localhost:5000/api/health`, simplificando o acesso e evitando configurar CORS para este percurso. Isto não configura automaticamente a arquitetura de produção.

No `App.tsx`, testar `fetch('/api/health')` e apresentar o resultado (o exemplo discutido mostrava `System status: ok - connected`). Este componente de teste será posteriormente substituído pelo layout e pelas páginas definitivas.

**Três terminais para testar o fluxo completo**, a partir da raiz:

```powershell
# Terminal 1 — base de dados
docker compose up -d
```

```powershell
# Terminal 2 — API
dotnet run --project backend/PersonalFinance.Api -- --urls http://localhost:5000
```

```powershell
# Terminal 3 — frontend
cd frontend
npm run dev
```

Endpoints:

- API: `http://localhost:5000/api/health` → `{"status":"ok","database":"connected"}`.
- React: `http://localhost:5173` → deve indicar ligação OK, caso o frontend implemente o teste.

## 8. Git, GitHub Projects, VS Code e CI

### 8.1 Organização

- **Repositório GitHub:** código, histórico de commits, Issues e Pull Requests.
- **GitHub Project:** quadro de planeamento de tarefas (o link partilhado na conversa apontava para este quadro, não para um repositório clonável).
- **`main`:** versão estável; **`develop`:** integração; **`feature/...`:** funcionalidades; **`fix/...`:** correções.
- **Pull Request:** pedido de integração com revisão cruzada antes do merge.

### 8.2 Erro do VS Code ao fazer commit

Mensagem encontrada: **“Make sure you configure your `user.name` and `user.email` in git.”**

Estar **autenticado no GitHub** não configura automaticamente o **autor dos commits locais**. Resolver no terminal do VS Code:

```powershell
git config --global user.name "O Teu Nome"
git config --global user.email "o-teu-email@exemplo.com"
git config --global user.name
git config --global user.email
```

Usar, se desejado, o endereço privado *noreply* configurado no GitHub.

Para registar e publicar alterações:

```powershell
git status
git add .
git commit -m "chore: initial project setup"
git push
```

Se ainda não houver branch remota associada, usar a opção **Publish Branch** do VS Code ou `git push -u origin NOME_DA_BRANCH`. Verificar que `.env` e outros segredos não aparecem nas alterações preparadas.

**Commit vs Push:** o commit cria um marco no histórico **local**; o push envia os commits para o **repositório remoto**.

### 8.3 `.gitignore` e README

O `.gitignore` deve excluir `.env`, `**/bin/`, `**/obj/`, `node_modules/`, `frontend/dist/` e ficheiros temporários. Não excluir `package-lock.json`, migrations, solution ou `.config/dotnet-tools.json`.

O `README.md` na raiz deve explicar a finalidade do projeto, estrutura de pastas, pré-requisitos, instalação, configuração, comandos para executar, testes, regras de colaboração e problemas comuns. Serve para facilitar a entrada de novos membros da equipa.

### 8.4 GitHub Actions (CI)

A conversa propôs `.github/workflows/ci.yml` para executar, em cada push/PR:

1. Instalar .NET 10 e Node 24.
2. Restaurar e compilar a solution .NET.
3. Executar `dotnet test`.
4. Executar `npm ci` e `npm run build` no frontend.

**Teoria:** *Continuous Integration* (CI) significa verificar alterações continuamente através de um processo automático. Evita integrar código que não compila ou que quebra testes. Uma pipeline verde **não garante** que toda a aplicação funciona: por enquanto não substitui testes de integração com PostgreSQL nem testes funcionais.

## 9. Planeamento e primeiros módulos

| Área principal | Responsabilidade sugerida |
|---|---|
| Frontend | React, layout, formulários, navegação. |
| Backend/autenticação | Identity, registo, login, perfil e controllers. |
| Regras de negócio | Contas, categorias, transações e orçamentos. |
| Persistência/integração | PostgreSQL, migrations, dados fictícios, testes e CI. |

Planeamento aproximado discutido:

- **Semana 1:** setup, arquitetura, modelo de dados, wireframes e contratos REST.
- **Semana 2:** autenticação, perfil, contas e categorias.
- **Semana 3:** transações e filtros.
- **Semana 4:** orçamentos, metas e dashboard.
- **Semana 5:** alertas/relatórios, testes e correções.
- **Semana 6:** estabilização, documentação e apresentação.

**Primeiro milestone recomendado:** utilizador regista-se → inicia sessão → cria conta financeira → dados são guardados no PostgreSQL → conta aparece no React. É um *fluxo vertical* porque atravessa todas as camadas da solução; valida a arquitetura antes de se adicionarem mais funcionalidades.

### Modelos previstos (além das tabelas técnicas do Identity)

`Account`, `Category`, `Transaction`, `Budget`, `SavingsGoal`, `GoalContribution`.

**Decisões financeiras importantes:**

- Utilizar `decimal` para montantes.
- Garantir que cada utilizador só consulta e modifica os seus dados: a chave estrangeira não é suficiente, é indispensável verificar autorização na API.
- Distinguir contas reais ficticiamente representadas na aplicação de **integração bancária real**, que ficou fora do âmbito.
- Decidir explicitamente se haverá transferências entre contas próprias e como o saldo é calculado.
- Tratar contribuições para metas de poupança como reservas virtuais, se essa for a regra de negócio adotada; uma meta não move dinheiro automaticamente.
- Para demonstrar recuperação de palavra-passe, pode usar-se email de testes (por exemplo, Mailpit), sem enviar mensagens reais.

## 10. Checklist para quem entra agora no projeto

> **Não recriar o projeto se o repositório já tiver código.** Seguir apenas os passos de instalação/execução.

- [ ] Git, .NET 10, Node 24, Docker e VS Code instalados.
- [ ] WSL 2 e Docker Desktop funcionais; `docker run --rm hello-world` passa.
- [ ] Repositório de código clonado a partir do URL correto.
- [ ] `.env` criado a partir de `.env.example`, com password própria.
- [ ] `docker compose up -d` inicia o PostgreSQL; `docker compose ps` indica `healthy`.
- [ ] User Secrets configurados com a connection string local.
- [ ] `dotnet restore` e `dotnet tool restore` executados (se houver manifesto de ferramentas).
- [ ] `dotnet ef database update` aplica as migrations existentes.
- [ ] `dotnet build` e `dotnet test` concluem sem erros.
- [ ] Em `frontend/`, `npm ci` instala dependências e `npm run dev` inicia o Vite.
- [ ] `http://localhost:5000/api/health` responde `ok/connected` (se o endpoint existir).
- [ ] `http://localhost:5173` abre o frontend; o teste de integração responde corretamente (se implementado).
- [ ] Nome e email do Git configurados; commit e push funcionam.
- [ ] Pipeline GitHub Actions funciona após enviar o código.

## 11. Vocabulário essencial

| Termo | Definição curta |
|---|---|
| **API** | Interface pela qual o frontend solicita operações ao backend. |
| **Endpoint** | URL/método específico de uma operação da API. |
| **Controller** | Componente que recebe pedidos HTTP e encaminha o trabalho. |
| **Service** | Componente que contém regras de negócio. |
| **DTO** | Objeto que define os dados de entrada/saída da API. |
| **Entity/Model** | Representação de um conceito de domínio (p. ex., uma conta). |
| **DbContext** | Unidade de trabalho do EF Core para consultar e guardar entidades. |
| **Migration** | Alteração versionada da estrutura da base de dados. |
| **Identity** | Framework de gestão de utilizadores/autenticação do ASP.NET Core. |
| **WSL 2** | Ambiente Linux em Windows, usado habitualmente pelo Docker Desktop. |
| **Image Docker** | Modelo imutável a partir do qual são criados contentores. |
| **Container** | Processo isolado criado a partir de uma imagem. |
| **Volume Docker** | Armazenamento persistente para contentores. |
| **Git commit** | Registo local de alterações no histórico. |
| **Git push** | Envio dos commits para um remoto. |
| **Pull Request** | Proposta de integração de alterações, sujeita a revisão. |
| **CI** | Execução automática de builds e testes quando há alterações. |

---

**Síntese:** a estratégia escolhida procura equilibrar **aprendizagem, simplicidade, colaboração e entrega em seis semanas**. O objetivo do setup não é construir toda a aplicação: é garantir que quatro pessoas conseguem trabalhar sobre o mesmo código e que existe um primeiro percurso técnico **React → API → PostgreSQL**, seguro e reproduzível, sobre o qual o restante produto pode crescer.
