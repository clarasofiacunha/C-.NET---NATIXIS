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

No desenvolvimento, o Vite encaminha `/auth` para a API local. Em produção, o frontend e a API devem estar no mesmo site através de reverse proxy ou ser configurados com `VITE_API_URL` e CORS. Como o cookie é `SameSite=Lax`, frontend e API precisam de estar no mesmo site para o fluxo funcionar corretamente. Produção deve usar HTTPS.

O frontend trata os erros da API e mostra mensagens adequadas: credenciais inválidas/conta bloqueada no login, mensagens de validação no registo e aviso quando a sessão termina.

## Recuperação de palavra-passe — serviço Brevo (task 1)

### Objetivo

O serviço prepara e envia um email transacional pela Brevo. Nesta fase ainda não existe um endpoint a chamá-lo: isso será feito na task seguinte. O serviço recebe o email do destinatário e o link de redefinição já criado, monta o pedido à Brevo e envia-o através de `HttpClient`.

### Ficheiros e responsabilidade

| Ficheiro | Responsabilidade |
| --- | --- |
| `Services/IRecoveryEmailSender.cs` | Define o contrato que qualquer serviço de envio de recuperação tem de cumprir. |
| `Services/BrevoRecoveryEmailSender.cs` | Implementa o contrato usando a API HTTP da Brevo. |
| `Services/BrevoOptions.cs` | Representa a configuração lida do servidor: chave Brevo, remetente e URL base do frontend. |
| `Services/RecoveryEmailDeliveryException.cs` | Define um erro controlado de envio, seguro para devolver/tratar sem revelar segredos. |
| `Program.cs` | Regista as opções e o cliente HTTP no contentor de injeção de dependências. |
| `PersonalFinance.Tests/BrevoRecoveryEmailSenderTests.cs` | Testa o pedido HTTP sem contactar a Brevo. |

### Contrato: `IRecoveryEmailSender`

```csharp
Task SendPasswordResetAsync(string recipientEmail, Uri resetLink, CancellationToken cancellationToken = default);
```

Uma interface permite que o controlador dependa de uma abstração, e não da Brevo diretamente. Isto torna o código mais fácil de testar: nos testes do endpoint futuro podemos substituir este serviço por uma implementação falsa, sem enviar emails reais.

- `recipientEmail`: endereço que recebe a mensagem.
- `resetLink`: URL já construída pelo fluxo de recuperação. O serviço não cria nem regista o token que possa existir nesse link.
- `CancellationToken`: permite cancelar o pedido, por exemplo se o cliente HTTP desligar.

### Configuração: `BrevoOptions`

`BrevoOptions` é uma classe simples usada pelo padrão Options do ASP.NET Core. A secção de configuração chama-se `Brevo`.

| Propriedade | Finalidade |
| --- | --- |
| `ApiKey` | Chave privada enviada apenas no header `api-key` do pedido para a Brevo. |
| `SenderEmail` | Email de remetente, que tem de estar verificado na conta Brevo para envios reais. |
| `SenderName` | Nome apresentado como remetente; é opcional. |
| `FrontendBaseUrl` | URL pública do frontend; será usada na task 2 para formar o link de recuperação. |

Em desenvolvimento, os valores podem ser definidos com `dotnet user-secrets`; em produção devem vir de variáveis de ambiente ou de uma secret store. A chave nunca deve estar no frontend, em `appsettings.json` versionado ou nos logs.

### Registo no `Program.cs`

```csharp
builder.Services.Configure<BrevoOptions>(
    builder.Configuration.GetSection(BrevoOptions.SectionName));
builder.Services.AddHttpClient<IRecoveryEmailSender, BrevoRecoveryEmailSender>(client =>
{
    client.BaseAddress = new Uri(BrevoRecoveryEmailSender.ApiBaseAddress);
});
```

O ASP.NET Core possui um contentor de injeção de dependências. `Configure` associa a secção `Brevo` a `BrevoOptions`. `AddHttpClient` cria e gere o `HttpClient` para `BrevoRecoveryEmailSender`, incluindo a base `https://api.brevo.com/`. Quando uma classe pedir `IRecoveryEmailSender`, recebe automaticamente uma instância de `BrevoRecoveryEmailSender` pronta a usar.

### Envio do pedido HTTP

O método `SendPasswordResetAsync` executa estes passos:

1. Lê e valida a configuração necessária: chave e email do remetente.
2. Confirma que recebeu destinatário e uma URL absoluta.
3. Cria o payload JSON esperado pela Brevo: `sender`, `to`, `subject` e `htmlContent`.
4. Cria um `HttpRequestMessage` `POST` para `v3/smtp/email` e coloca a chave apenas no header `api-key`.
5. Envia o pedido com `httpClient.SendAsync`.
6. Se a Brevo responder com código 2xx, termina com sucesso; caso contrário lança `RecoveryEmailDeliveryException`.

O corpo HTML usa `WebUtility.HtmlEncode(resetLink.AbsoluteUri)`. Esta codificação evita que caracteres especiais do URL sejam interpretados como HTML. Por exemplo, `&` fica `&amp;` dentro do atributo `href`, mas o browser reconstrói o link corretamente quando a pessoa clica.

### Tratamento seguro de erros

Quando a configuração está incompleta ou a Brevo devolve uma resposta de erro, o serviço lança `RecoveryEmailDeliveryException` com uma mensagem genérica. Para falhas de rede, converte `HttpRequestException` no mesmo tipo de erro controlado.

Os logs registam apenas o código de estado HTTP ou que o fornecedor não foi alcançado. Não registam o URL do pedido, corpo JSON, email do destinatário, token de recuperação ou chave da API. Isto é importante porque o token dá acesso à alteração da palavra-passe.

O cancelamento real de um pedido não é transformado num erro de envio: `OperationCanceledException` é propagada quando o `CancellationToken` foi cancelado.

### Testes automatizados

Os testes usam `RecordingHandler`, uma implementação falsa de `HttpMessageHandler`. Em vez de a chamada sair para a internet, este handler guarda o pedido que recebeu e devolve uma resposta programada.

São cobertos três casos:

1. Resposta `202 Accepted`: confirma método `POST`, URL da Brevo, header `api-key`, remetente, destinatário e conteúdo do email.
2. Resposta `400 Bad Request`: confirma que é lançado um erro controlado e que a mensagem não inclui o token.
3. Configuração sem chave/remetente: confirma que o serviço falha de forma controlada antes de enviar um pedido.

Assim, a equipa consegue testar a integração sem credenciais Brevo e sem enviar mensagens reais.
