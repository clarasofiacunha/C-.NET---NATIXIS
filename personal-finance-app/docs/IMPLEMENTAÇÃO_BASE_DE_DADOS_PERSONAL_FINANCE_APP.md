# Personal Finance App — Resumo e guia da base de dados

**Contexto:** configuração da base de dados em ambiente de desenvolvimento, no Windows com PowerShell, para uma aplicação web com backend ASP.NET Core, Entity Framework Core (EF Core) e PostgreSQL em Docker.

**Objetivo deste documento:** explicar as decisões técnicas, registar os problemas de ligação encontrados nesta conversa e disponibilizar um passo-a-passo reproduzível para qualquer membro da equipa.

> **Estado conhecido no fim da conversa:** o contentor `personal-finance-db` foi confirmado como **Running**. O erro de autenticação foi diagnosticado: os comandos anteriores utilizavam `finance_dev`, mas o `.env` fornecido define `personal_dev`.

---

## 1. Arquitetura: o que é cada componente?

```text
Frontend (React)
      |
      | HTTP / API
      v
Backend (ASP.NET Core, C#)
      |
      | Entity Framework Core + Npgsql
      v
PostgreSQL (contentor Docker)
      ^
      |
DBeaver (cliente para visualizar/consultar a BD)
```

- **PostgreSQL:** sistema de gestão de base de dados relacional (SGBD). Guarda utilizadores, contas, categorias, transações, orçamentos e metas em tabelas.
- **Docker:** executa o PostgreSQL de forma isolada, sem instalar o servidor diretamente no Windows.
- **Docker Compose:** utiliza um ficheiro `docker-compose.yml` para descrever serviço, imagem, portas, variáveis de ambiente, volume e verificação de saúde.
- **Entity Framework Core:** ORM (*Object–Relational Mapper*), que mapeia classes C# para tabelas e facilita consultas e alterações.
- **Npgsql:** fornecedor que liga EF Core ao PostgreSQL.
- **Migrations:** ficheiros versionados de evolução do esquema (estrutura) da base de dados.
- **ASP.NET Core Identity:** infraestrutura de autenticação/utilizadores; gere hashes de passwords e tabelas auxiliares, mas os endpoints de autenticação precisam de ser implementados/configurados.
- **DBeaver:** aplicação gráfica que funciona como cliente da BD. Não é o servidor nem é necessário mantê-la aberta para a API funcionar.
- **Bogus / seed:** biblioteca e rotina C# utilizadas para criar informação fictícia de demonstração e testes.

### Modelo de dados do MVP

| Tabela de negócio | Finalidade | Relações principais |
| --- | --- | --- |
| `AspNetUsers` (Identity) | Contas de utilizador e credenciais em hash | Um utilizador tem vários registos financeiros |
| `Accounts` | Contas financeiras, por exemplo à ordem e poupança | `UserId` |
| `Categories` | Categorias de despesas ou receitas | `UserId` |
| `Transactions` | Receitas e despesas, com montante/data/descrição | `UserId`, `AccountId`, `CategoryId` |
| `Budgets` | Limites por categoria, mês e ano | `UserId`, `CategoryId` |
| `Goals` | Metas de poupança | `UserId` |

O nome das tabelas deve ser confirmado no projeto e na migration instalada; os exemplos acima correspondem ao modelo de partida discutido. O Identity pode criar também tabelas como `AspNetRoles`, `AspNetUserRoles` e outras.

**Regras de modelação:** valores monetários em `decimal` (por exemplo precisão `18,2`); chaves estrangeiras para manter integridade referencial; transações de valor positivo e tipo receita/despesa; índice único para impedir dois orçamentos iguais para o mesmo utilizador/categoria/mês/ano; validação da propriedade dos recursos no backend. O saldo de uma conta pode ser calculado a partir do saldo inicial mais receitas menos despesas. Regras como «orçamento só em categoria de despesa» exigem validação de negócio se não estiverem implementadas diretamente na BD.

---

## 2. Configuração real partilhada nesta conversa

### `.env` (na raiz do repositório)

```dotenv
POSTGRES_DB=personal_finance
POSTGRES_USER=personal_dev
POSTGRES_PASSWORD=dev_only_change_me
```

Estas credenciais são **apenas de desenvolvimento local** e devem ser substituídas em ambientes públicos ou partilhados. Não enviar o ficheiro `.env` para o GitHub. O `.env.example` deve conter valores de exemplo seguros, sem segredos reais.

### `docker-compose.yml` (resumo dos campos relevantes)

```yaml
services:
  db:
    image: postgres:17
    container_name: personal-finance-db
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "127.0.0.1:5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

O ficheiro original também inclui `restart: unless-stopped` e um `healthcheck` com `pg_isready`.

**Três nomes distintos:**

1. **Serviço Compose:** `db` — usar em `docker compose exec db ...`.
2. **Nome do contentor Docker:** `personal-finance-db` — usar em `docker exec -it personal-finance-db ...`.
3. **Utilizador PostgreSQL:** `personal_dev` — usar em `psql -U personal_dev` e na connection string.

**Por que o volume é importante?** `postgres_data` persiste os dados mesmo que o contentor seja reiniciado ou recriado. As variáveis `POSTGRES_USER` e `POSTGRES_PASSWORD` são aplicadas na **primeira inicialização de um diretório de dados vazio**; mudar o `.env` posteriormente não muda automaticamente os utilizadores/passwords já gravados nesse volume.

---

## 3. Passo-a-passo para pôr a base de dados a funcionar

> **Convenção:** executar os comandos no **PowerShell**. Os passos 3.1–3.3 e 3.8 usam a raiz `personal-finance-app/`. Os passos de EF Core e User Secrets podem ser executados da raiz com `--project`, conforme os exemplos.

### 3.1. Iniciar o Docker Desktop e o PostgreSQL

Abrir o Docker Desktop e, no terminal na raiz do repositório:

```powershell
docker compose up -d
docker compose ps
```

**Explicação teórica:** `up -d` cria/inicia os serviços em segundo plano; `ps` apresenta o estado e as portas. Esperar que `personal-finance-db` esteja a executar, idealmente com estado `healthy`.

**Resultado visto na conversa:** `Container personal-finance-db Running`.

### 3.2. Verificar os nomes definidos no Compose

```powershell
docker compose config --services
docker compose ps
```

**Explicação:** `config --services` mostra os **serviços** (neste caso `db`), não os nomes visíveis dos contentores. É por isso que `docker compose exec postgres ...` devolveu `service "postgres" is not running`: **não existe um serviço chamado `postgres` nesta configuração**.

### 3.3. Testar o acesso à base de dados pelo terminal

```powershell
docker compose exec db psql -U personal_dev -d personal_finance
```

Comando equivalente, usando o nome explícito do contentor:

```powershell
docker exec -it personal-finance-db psql -U personal_dev -d personal_finance
```

**Explicação:** `psql` é o cliente de consola do PostgreSQL. O parâmetro `-U` escolhe o utilizador; `-d` escolhe a base de dados. Dentro do contentor a ligação pode usar um **socket local**, pelo que este teste não substitui necessariamente um teste de autenticação TCP efetuado pelo backend/DBeaver.

Já dentro de `psql`:

```sql
SELECT current_user;
SELECT current_database();
```

Resultados esperados: `personal_dev` e `personal_finance`.

Sair:

```text
\q
```

**Se o utilizador `personal_dev` não existir:** não apagar volumes precipitadamente. É possível que o volume tenha sido inicializado com outra configuração. Inspecionar a configuração/histórico e, com credenciais de administrador válidas, criar ou corrigir a conta. Não assumir que o utilizador `postgres` existe sempre: se foi definido outro superutilizador na inicialização, este pode ser o único papel administrativo inicial.

### 3.4. Configurar os User Secrets do ASP.NET Core

Na raiz do repositório:

```powershell
dotnet user-secrets init --project backend/PersonalFinance.Api
```

Depois, configurar a connection string com os **mesmos valores** do PostgreSQL:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Host=localhost;Port=5432;Database=personal_finance;Username=personal_dev;Password=dev_only_change_me' --project backend/PersonalFinance.Api
```

Confirmar se a chave existe:

```powershell
dotnet user-secrets list --project backend/PersonalFinance.Api
```

**Atenção:** o último comando pode apresentar a password em texto legível. **Não partilhar o resultado sem ocultar a password.**

**Explicação teórica:** `DefaultConnection` é o nome da ligação que o `Program.cs` deverá procurar com `GetConnectionString("DefaultConnection")`. Os *User Secrets* guardam credenciais fora do repositório, por utilizador/máquina, quando a aplicação corre no ambiente `Development`. Não são um cofre de produção. `localhost:5432` funciona porque o Docker publica a porta `5432` do contentor no próprio computador.

### 3.5. Garantir que o backend tem as dependências necessárias

A configuração de referência do README usa **.NET 10, EF Core 10 e PostgreSQL 17**. Verificar os pacotes no `.csproj`, em vez de instalar versões de EF Core 8 ou 9 num projeto .NET 10:

```powershell
dotnet restore PersonalFinance.slnx
dotnet tool restore
dotnet build PersonalFinance.slnx
```

**Explicação:** `dotnet restore` resolve as dependências NuGet; `dotnet tool restore` instala as ferramentas locais (incluindo `dotnet-ef`, caso esteja declarada); `dotnet build` verifica a compilação C#. Compilar com sucesso **não prova que a ligação à base de dados funcione**.

### 3.6. Aplicar as migrations (criar/atualizar tabelas)

Na raiz:

```powershell
dotnet ef database update --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

**Explicação:** EF Core compara as migrations do projeto com o histórico da base de dados (tabela `__EFMigrationsHistory`) e aplica as alterações em falta. Este comando é diferente de criar uma migration.

**Apenas quem prepara o esquema inicial deve criar a primeira migration, uma vez, após implementar entidades e DbContext:**

```powershell
dotnet ef migrations add InitialCreate --project backend/PersonalFinance.Api --startup-project backend/PersonalFinance.Api
```

A pasta `Migrations/` deve ser versionada no GitHub para que os restantes membros executem somente `database update`. **Não gerar outra `InitialCreate` por computador.**

**O que aconteceu nesta conversa?** `dotnet ef database update` apresentou `Build succeeded`, seguido de `Npgsql.PostgresException 28P01: password authentication failed for user "finance_dev"`. Portanto, a compilação passou mas a ligação com esse utilizador não foi aceite. A solução proposta foi corrigir a connection string para `personal_dev`, de acordo com o `.env` fornecido. Não existe nesta conversa prova de que a atualização foi posteriormente concluída.

### 3.7. Criar dados fictícios (seed)

**Somente se `DemoSeeder` estiver implementado e ligado ao `Program.cs` e a migration já tiver sido aplicada**, executar:

```powershell
dotnet run --project backend/PersonalFinance.Api -- --seed
```

**Explicação:** `seed` é o preenchimento inicial de uma BD com registos de teste. O gerador discutido utiliza **Bogus** para produzir valores fictícios de quatro perfis financeiros, com contas, categorias, receitas, despesas, orçamentos e metas de poupança. Os valores são **plausíveis/ilustrativos**, não dados bancários de pessoas reais nem estatísticas oficiais sobre Portugal. O exemplo gera aproximadamente 12 meses de dados e contas de demonstração.

Se a implementação de exemplo do `DemoSeeder` ainda for a utilizada, as contas demonstrativas são `demo1@example.test` até `demo4@example.test`, com a password de exemplo `Demo123!Change`. Não disponibilizar essas contas em produção. O gerador original evita voltar a criar os movimentos de um utilizador quando já existe uma conta financeira desse utilizador; **não é um mecanismo de sincronização de alterações existentes**.

### 3.8. Confirmar as tabelas e os dados

Opção A — terminal PostgreSQL:

```powershell
docker compose exec db psql -U personal_dev -d personal_finance
```

Dentro de `psql`:

```sql
\dt
SELECT COUNT(*) FROM "AspNetUsers";
SELECT COUNT(*) FROM "Transactions";
```

**Explicação:** `\dt` lista tabelas, e `COUNT(*)` conta registos. `AspNetUsers` pertence ao Identity e `Transactions` contém movimentos financeiros. Uma tabela vazia após a migração é normal **antes** de executar o seed.

Opção B — DBeaver: ver secção 4.

---

## 4. Ligar o DBeaver ao PostgreSQL

1. Abrir o DBeaver.
2. Escolher **Database → New Database Connection**.
3. Selecionar **PostgreSQL** e prosseguir.
4. Preencher os seguintes dados:

   | Campo | Valor para a configuração atual |
   | --- | --- |
   | Host | `localhost` |
   | Port | `5432` |
   | Database | `personal_finance` |
   | Username | `personal_dev` |
   | Password | O valor atual de `POSTGRES_PASSWORD` no `.env` (`dev_only_change_me` no exemplo desta conversa) |

5. Clicar em **Test Connection**. Se pedir o driver JDBC PostgreSQL, aceitar o download.
6. Se aparecer **Connected**, clicar **Finish**.
7. No painel lateral, expandir a ligação e procurar **Databases → personal_finance → Schemas → public → Tables** (a estrutura de navegação pode variar por versão).
8. Clicar com o botão direito numa tabela → **View Data → All Rows**; ou abrir o **SQL Editor → New SQL Script**.

Consultas úteis no editor do DBeaver:

```sql
-- Listar tabelas públicas:
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;

-- Mostrar até 20 transações:
SELECT * FROM "Transactions" LIMIT 20;

-- Resumir transações por mês e tipo:
SELECT DATE_TRUNC('month', "Date") AS mes,
       "Type", SUM("Amount") AS total
FROM "Transactions"
GROUP BY DATE_TRUNC('month', "Date"), "Type"
ORDER BY mes, "Type";
```

**Explicação:** o DBeaver liga-se pela porta TCP `localhost:5432`. As aspas duplas preservam a capitalização dos nomes de tabelas/colunas criados pelo EF Core no PostgreSQL. Na ausência de aspas, o PostgreSQL converte identificadores para minúsculas.

**Importante:** DBeaver é opcional. Se estiver fechado, a API pode continuar a funcionar normalmente.

---

## 5. Os erros encontrados: causa e correção

### Erro A — `28P01: password authentication failed for user "finance_dev"`

- **Comando envolvido:** `dotnet ef database update`.
- **Diagnóstico:** o EF Core compilou, mas o PostgreSQL rejeitou a autenticação com o utilizador `finance_dev`.
- **Evidência:** o `.env` partilhado define `POSTGRES_USER=personal_dev`, enquanto a string de ligação usada inicialmente tinha `Username=finance_dev`.
- **Correção recomendada:** configurar `DefaultConnection` com `Username=personal_dev` e a password correspondente; confirmar que esse utilizador existe no volume atual.
- **Teoria:** utilizador PostgreSQL (*role*) e utilizador da aplicação são conceitos distintos. A role é um principal de autenticação do servidor PostgreSQL; o utilizador da aplicação é um registo na tabela `AspNetUsers`.

### Erro B — `service "postgres" is not running`

- **Comando que falhou:** `docker compose exec postgres psql -U finance_dev -d personal_finance`.
- **Causa:** não existe um serviço Compose denominado `postgres`; o `docker-compose.yml` define `services: db:`.
- **Comando correto:** `docker compose exec db psql -U personal_dev -d personal_finance`.
- **Teoria:** `docker compose exec` recebe o **nome do serviço**; `docker exec` recebe o **nome/ID do contentor**.

### Erro C — `FATAL: role "finance_dev" does not exist`

- **Comando que falhou:** `docker exec -it personal-finance-db psql -U finance_dev -d personal_finance`.
- **Causa:** o próprio PostgreSQL informou que a role solicitada não existe naquele cluster.
- **Comando a tentar:** `docker exec -it personal-finance-db psql -U personal_dev -d personal_finance`.
- **Teoria:** o nome do contentor estava correto, mas o utilizador do banco de dados estava errado.

### Se a autenticação com `personal_dev` ainda falhar

Pode existir um volume PostgreSQL inicializado anteriormente com outros utilizadores/passwords. Alterar o `.env` ou executar `docker compose up -d` não altera credenciais já guardadas nesse volume. Um administrador da BD pode corrigir a role/password **depois de confirmar o estado real do cluster**. Evitar comandos de destruição para resolver um problema de credenciais.

---

## 6. Boas práticas para toda a equipa

1. **Alinhar as credenciais:** os valores do `.env`, do User Secrets e da ligação DBeaver devem apontar para o mesmo servidor/utilizador/BD.
2. **Credenciais privadas:** não versionar `.env` nem User Secrets; usar `.env.example` apenas como template. Nunca partilhar passwords nos logs/Issues.
3. **Migrations versionadas:** uma migração inicial criada e comitada por um responsável; todos os colegas aplicam-na localmente.
4. **Dados sintéticos:** não utilizar extratos bancários pessoais para demonstração. É possível calibrar distribuições de despesas com dados públicos agregados do INE/PORDATA, identificando claramente as fontes e sem tratar parâmetros inventados como estatísticas reais.
5. **Separação por utilizador:** todos os endpoints de contas, transações e orçamentos devem validar o dono dos registos, mesmo que existam chaves estrangeiras e índices na BD.
6. **Dinheiro:** `decimal`, precisão e regras consistentes para evitar erros de arredondamento.
7. **Não apagar dados por engano:** `docker compose down` para contentores; **não executar `docker compose down -v`**, porque `-v` remove os volumes associados ao projeto e pode destruir a base de dados local.
8. **Diagnóstico progressivo:** verificar contentor → serviço → role/BD → User Secrets → migrations → seed → DBeaver.

## 7. Referências no repositório

- `README.md` — guia principal de instalação e desenvolvimento.
- `.env.example` — modelo de variáveis locais, sem segredos.
- `docker-compose.yml` — declaração do serviço `db`, porta e volume PostgreSQL.
- `backend/PersonalFinance.Api/Program.cs` — configuração do `AppDbContext`, ligação Npgsql e eventual registo do seed.
- `backend/PersonalFinance.Api/Data/AppDbContext.cs` — classes/tabelas e relações.
- `backend/PersonalFinance.Api/Data/DemoSeeder.cs` — dados fictícios, se implementados.
- `backend/PersonalFinance.Api/Migrations/` — histórico versionado do esquema.

**Nota sobre endpoints de diagnóstico:** no ficheiro `Program.cs` anteriormente partilhado no contexto do projeto, o endpoint de saúde aparecia como `/health`, enquanto o README menciona `/api/health`. Confirmar qual é a rota **atualmente implementada** e alinhar o README; não considerar um `404` numa rota incorreta como prova de falha da base de dados.

**Estado final do diagnóstico:** a configuração real fornecida estabelece que o nome correto da role pretendida é `personal_dev` e o serviço Compose é `db`. A execução bem-sucedida das migrations, do seed e da ligação gráfica permanece por confirmar no ambiente local.
