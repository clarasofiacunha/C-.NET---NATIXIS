# Guia — Autenticação

Este guia reúne a explicação da funcionalidade de autenticação: registo, login, sessão por cookie, logout e recuperação de palavra-passe. Deve ser atualizado à medida que forem concluídas novas tasks deste fluxo.

## Visão geral do fluxo

```text
Registo ──> conta criada ──> Login ──> cookie `pf.auth` ──> rotas protegidas
                                      │
                                      ├──> GET /auth/me: obtém utilizador da sessão
                                      └──> POST /auth/logout: termina a sessão

Esqueceu a palavra-passe ──> email com link ──> nova palavra-passe (em implementação)
```

O backend usa ASP.NET Core Identity para gerir utilizadores e passwords. As passwords nunca são guardadas em texto simples: o Identity cria e guarda o respetivo hash. A autenticação atual é baseada num cookie de sessão, não em JWT.

## Estrutura do código

| Ficheiro | Responsabilidade |
| --- | --- |
| `Models/AppUser.cs` | Representa o utilizador da aplicação e estende `IdentityUser<Guid>`. |
| `Controllers/AuthController.cs` | Expõe os endpoints de registo, login, sessão e logout. |
| `DTOs/Auth/` | Define os dados aceites e devolvidos pelos endpoints de autenticação. |
| `Program.cs` | Configura Identity, política de password, cookie e autorização. |
| `frontend/src/pages/auth/` | Contém as páginas e formulários React de autenticação. |
| `frontend/src/pages/auth/auth.ts` | Centraliza os pedidos HTTP do frontend para `/auth`. |

## Utilizador: `AppUser`

`AppUser` herda de `IdentityUser<Guid>`. Ao herdar esta classe, recebe propriedades e comportamento do Identity, como `Id`, `Email`, `UserName`, hash da password, controlo de tentativas falhadas e gestão de tokens. O projeto acrescenta `Name`, `CreatedAt` e as coleções de contas, categorias, transações, orçamentos e metas que pertencem ao utilizador.

Usar `Guid` como identificador torna cada utilizador identificado por um valor único, em vez de um número sequencial previsível.

## Registo — `POST /auth/register`

O frontend envia `name`, `email` e `password`. Estes dados entram no `RegisterRequestDto`, que usa atributos de validação:

- nome obrigatório, entre 2 e 150 caracteres;
- email obrigatório, com formato de email e até 256 caracteres;
- password obrigatória, entre 8 e 100 caracteres.

No `AuthController`, o email e nome são limpos com `Trim()`. É criado um `AppUser`, com `UserName` e `Email` iguais ao email introduzido e `LockoutEnabled = true`. Em seguida, `userManager.CreateAsync(user, password)` cria a conta.

O `UserManager` pertence ao ASP.NET Core Identity. Ele valida as regras de password, calcula o hash seguro da password e guarda o utilizador. Se falhar, a API devolve `400 Bad Request` com os erros agrupados por código. Se resultar, devolve `201 Created` com o identificador, nome e email — nunca a password nem o hash.

Criar a conta não inicia automaticamente uma sessão; a pessoa faz login de seguida.

## Login — `POST /auth/login`

O pedido recebe `email` e `password` através de `LoginRequestDto`. Primeiro, `userManager.FindByEmailAsync` procura o utilizador. Se não existir, a API devolve `401 Unauthorized` com a mesma mensagem usada para uma password incorreta. Esta escolha evita revelar se determinado email tem conta.

Se o utilizador existir, `signInManager.PasswordSignInAsync` verifica a password e inicia a sessão. O parâmetro `lockoutOnFailure: true` conta tentativas falhadas. Depois de cinco tentativas inválidas, a conta fica bloqueada durante 15 minutos.

Quando o login resulta, a API responde com os dados públicos do utilizador e envia o cookie de autenticação. O browser guarda esse cookie e envia-o nos pedidos seguintes.

## Sessão, utilizador atual e logout

| Endpoint | Proteção | Comportamento |
| --- | --- | --- |
| `GET /auth/me` | `[Authorize]` | Lê o utilizador associado ao cookie e devolve os seus dados. Sem sessão devolve `401`. |
| `POST /auth/logout` | `[Authorize]` | `SignInManager.SignOutAsync()` remove/invalida a sessão e devolve `204 No Content`. |

O atributo `[Authorize]` impede que o método seja executado sem um utilizador autenticado. `userManager.GetUserAsync(User)` converte o `ClaimsPrincipal` da sessão no `AppUser` correspondente.

## Configuração de segurança no `Program.cs`

O projeto configura o Identity com email único, passwords de pelo menos oito caracteres e sem obrigatoriedade de símbolo. O bloqueio de conta permite cinco falhas e dura 15 minutos.

O cookie de autenticação chama-se `pf.auth`, é `HttpOnly` (não pode ser lido por JavaScript), usa `SameSite=Lax` e expira ao fim de oito horas, renovando enquanto a pessoa continua ativa. Em produção é enviado apenas por HTTPS; em Development e Testing pode usar HTTP para desenvolvimento local.

`AddDefaultTokenProviders()` ativa os fornecedores de tokens do Identity. A recuperação de palavra-passe usa um desses fornecedores, em vez de criar tokens próprios ou uma tabela nova.

## Frontend e comunicação com a API

O ficheiro `frontend/src/pages/auth/auth.ts` contém `authRequest`, a função usada pelas páginas de autenticação. Esta função usa `fetch`, envia JSON para `POST`, aplica um limite de 15 segundos e define `credentials: 'include'`. Esta última opção é essencial: permite que o browser envie e aceite o cookie de sessão.

No desenvolvimento, o Vite encaminha `/auth` para a API local em `http://localhost:5185`, que é a porta do perfil `http` do .NET. Se a API estiver a correr pelo Docker Compose, inicia o frontend com `$env:VITE_API_PROXY_TARGET = 'http://localhost:8080'` antes de `npm run web:dev`. Em produção, o frontend e a API devem estar no mesmo site através de reverse proxy ou ser configurados com `VITE_API_URL` e CORS. Como o cookie é `SameSite=Lax`, frontend e API precisam de estar no mesmo site para o fluxo funcionar corretamente. Produção deve usar HTTPS.

O frontend trata os erros da API e mostra mensagens adequadas: credenciais inválidas/conta bloqueada no login, mensagens de validação no registo e aviso quando a sessão termina.

## Recuperação de palavra-passe

### Objetivo

Uma pessoa introduz o email em `#forgot-password`. A API cria um token de redefinição do ASP.NET Core Identity e envia, através do relay SMTP da Brevo, um link para `#reset-password`. A página lê o `email` e o `token` do fragmento do URL, pede a nova palavra-passe duas vezes e chama `POST /auth/reset-password`.

O token usa o prazo padrão do provider do ASP.NET Core Identity. Também deixa de ser válido depois de usado ou quando a palavra-passe é alterada. Após uma alteração bem-sucedida, o frontend remove o token do URL antes de oferecer o regresso ao login.

### Ficheiros e responsabilidade

| Ficheiro | Responsabilidade |
| --- | --- |
| `Services/IRecoveryEmailSender.cs` | Define o contrato que qualquer serviço de envio de recuperação tem de cumprir. |
| `Services/BrevoRecoveryEmailSender.cs` | Implementa o contrato usando o relay SMTP da Brevo. |
| `Services/BrevoOptions.cs` | Representa a configuração lida do servidor: credenciais SMTP, remetente e URL base do frontend. |
| `Services/RecoveryEmailDeliveryException.cs` | Define um erro controlado de envio, seguro para devolver/tratar sem revelar segredos. |
| `Program.cs` | Regista as opções, a fábrica SMTP e o serviço no contentor de injeção de dependências. |
| `PersonalFinance.Tests/BrevoRecoveryEmailSenderTests.cs` | Testa o envio SMTP com um cliente falso, sem contactar a Brevo. |

### Contrato: `IRecoveryEmailSender`

```csharp
Task SendPasswordResetAsync(string recipientEmail, Uri resetLink, CancellationToken cancellationToken = default);
```

Uma interface permite que o controlador dependa de uma abstração, e não da Brevo diretamente. Isto torna o código fácil de testar: os testes substituem o serviço por uma implementação falsa, sem enviar emails reais.

- `recipientEmail`: endereço que recebe a mensagem.
- `resetLink`: URL já construída pelo fluxo de recuperação. O serviço não cria nem regista o token que possa existir nesse link.
- `CancellationToken`: permite cancelar o pedido se o cliente desligar.

### Configuração: `BrevoOptions`

`BrevoOptions` é uma classe simples usada pelo padrão Options do ASP.NET Core. A secção de configuração chama-se `Brevo`.

| Propriedade | Finalidade |
| --- | --- |
| `SmtpHost` | Host SMTP da Brevo, normalmente `smtp-relay.brevo.com`. |
| `SmtpPort` | Porta SMTP, normalmente `587`. |
| `SmtpUsername` | Utilizador SMTP da Brevo. |
| `SmtpPassword` | Chave SMTP privada da Brevo. |
| `SenderEmail` | Email de remetente, que tem de estar verificado na conta Brevo para envios reais. |
| `SenderName` | Nome apresentado como remetente; é opcional. |
| `FrontendBaseUrl` | URL pública do frontend, usada para formar o link de recuperação. |

Esta versão usa o relay SMTP da Brevo, não a API HTTP; por isso `Brevo:ApiKey` não é uma configuração usada. Em desenvolvimento, os valores podem ser definidos com `dotnet user-secrets`; em produção devem vir de variáveis de ambiente ou de uma secret store. As credenciais nunca devem estar no frontend, em `appsettings.json` versionado ou nos logs.

### Registo no `Program.cs`

```csharp
builder.Services.Configure<BrevoOptions>(
    builder.Configuration.GetSection(BrevoOptions.SectionName));
builder.Services.AddSingleton<ISmtpEmailClientFactory, SmtpEmailClientFactory>();
builder.Services.AddScoped<IRecoveryEmailSender, BrevoRecoveryEmailSender>();
```

O ASP.NET Core possui um contentor de injeção de dependências. `Configure` associa a secção `Brevo` a `BrevoOptions`. A fábrica cria o cliente SMTP com TLS; quando uma classe pede `IRecoveryEmailSender`, recebe uma instância de `BrevoRecoveryEmailSender` pronta a usar.

### Envio SMTP e ligação segura

O método `SendPasswordResetAsync` executa estes passos:

1. Lê e valida a configuração necessária: host, porta, utilizador, palavra-passe e email do remetente.
2. Confirma que recebeu destinatário e uma URL absoluta.
3. Cria a mensagem HTML e codifica o URL para o atributo `href`.
4. Cria um cliente SMTP com TLS e envia a mensagem.
5. Se o relay rejeitar a mensagem, devolve `RecoveryEmailDeliveryException`.

O corpo HTML usa `WebUtility.HtmlEncode(resetLink.AbsoluteUri)`. Esta codificação evita que caracteres especiais do URL sejam interpretados como HTML. Por exemplo, `&` fica `&amp;` dentro do atributo `href`, mas o browser reconstrói o link corretamente quando a pessoa clica.

### Tratamento seguro de erros

Quando a configuração está incompleta ou o relay SMTP devolve um erro, o serviço lança `RecoveryEmailDeliveryException` com uma mensagem genérica.

Os logs não registam o URL do pedido, email do destinatário, token de recuperação, utilizador SMTP ou palavra-passe SMTP. Isto é importante porque o token dá acesso à alteração da palavra-passe.

O cancelamento real de um pedido não é transformado num erro de envio: `OperationCanceledException` é propagada quando o `CancellationToken` foi cancelado.

### Testes automatizados

Os testes usam um cliente SMTP e um serviço de email falsos. Em vez de a chamada sair para a internet, guardam a mensagem recebida ou simulam uma falha de entrega.

São cobertos três casos:

1. Conta inexistente: devolve a mesma mensagem genérica e não envia email.
2. Token válido: altera a palavra-passe sem criar sessão; token inválido ou reutilizado falha.
3. Falha SMTP: confirma que o serviço devolve erro controlado e que o endpoint mantém a resposta genérica sem expor o token.
4. Configuração incompleta: confirma que o serviço falha de forma controlada antes de enviar uma mensagem.

Assim, a equipa consegue testar a integração sem credenciais Brevo e sem enviar mensagens reais.

### Configuração por ambiente

| Configuração ASP.NET Core | Variável de ambiente / segredo | Exemplo |
| --- | --- | --- |
| `Brevo:SmtpHost` | `Brevo__SmtpHost` | `smtp-relay.brevo.com` |
| `Brevo:SmtpPort` | `Brevo__SmtpPort` | `587` |
| `Brevo:SmtpUsername` | `Brevo__SmtpUsername` | utilizador SMTP da Brevo |
| `Brevo:SmtpPassword` | `Brevo__SmtpPassword` | chave SMTP da Brevo |
| `Brevo:SenderEmail` | `Brevo__SenderEmail` | `no-reply@exemplo.pt` |
| `Brevo:SenderName` | `Brevo__SenderName` | `Personal Finance` |
| `Brevo:FrontendBaseUrl` | `Brevo__FrontendBaseUrl` | `https://app.exemplo.pt/` |

O endereço em `Brevo:SenderEmail` tem de estar verificado na conta Brevo antes de qualquer envio real. `Brevo:FrontendBaseUrl` deve ser o URL público exato do frontend, com `http` ou `https`; não é derivado do cabeçalho `Host` do pedido.

Para Docker, use os valores do `.env` local a partir de `.env.example`; `docker-compose.yml` mapeia-os para `Brevo__...`. Sem Docker, use Secret Manager no projeto da API, por exemplo:

```powershell
dotnet user-secrets set "Brevo:SmtpPassword" "<chave-smtp>" --project backend/PersonalFinance.Api
dotnet user-secrets set "Brevo:SenderEmail" "no-reply@exemplo.pt" --project backend/PersonalFinance.Api
dotnet user-secrets set "Brevo:FrontendBaseUrl" "http://localhost:5173/" --project backend/PersonalFinance.Api
```

Em produção, guarde todos os valores num secret store ou nas variáveis do serviço de alojamento, usando a forma `Brevo__...`; restrinja o acesso à identidade da API e rode a palavra-passe SMTP em caso de exposição. Defina o URL HTTPS público do frontend e permita essa origem na política CORS da API.

O envio real permanece pendente até existirem credenciais SMTP e remetente verificado. Quando disponíveis, use uma conta de teste: peça a recuperação, abra o link, redefina a palavra-passe e confirme o login com a nova palavra-passe. Não registe nem partilhe o token do link.
