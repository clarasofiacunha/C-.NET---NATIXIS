# Autenticação

Em desenvolvimento, iniciar a API com `dotnet run --launch-profile http` na pasta
`backend/PersonalFinance.Api` e o frontend com `npm run dev` nesta pasta.
A API necessita da ligação PostgreSQL configurada e das migrações aplicadas.

O Vite encaminha `/auth` para `http://localhost:5185`. Os pedidos incluem cookies:
o login cria a sessão, `/auth/me` recupera-a ao abrir a página e o logout termina-a.
O registo cria a conta e pede ao utilizador que faça login.

Em produção, servir `/auth` na mesma origem através de um reverse proxy ou definir
`VITE_API_URL` no build (ver `.env.example`) e autorizar a origem no CORS da API.
A política SameSite=Lax atual requer que frontend e API estejam no mesmo site
para que a autenticação por cookie funcione. Usar HTTPS em produção.
