# PostgreSQL na Neon e deploy no Render

O backend usa PostgreSQL com EF Core/Npgsql. O projeto Neon vinculado localmente não guarda
credenciais no Git; `.neon` e `.env.neon` permanecem ignorados.

## Variáveis do banco

- `DATABASE_URL`: conexão pooled da Neon usada pela API.
- `DATABASE_URL_UNPOOLED`: conexão direta usada pelas migrations do EF Core.

Para atualizar o arquivo local após selecionar a branch Neon:

```bash
neon env pull --file .env.neon --service postgres
```

Para aplicar migrations manualmente, carregue `DATABASE_URL_UNPOOLED` no ambiente e execute:

```bash
dotnet ef database update --context AgendaiFisioDbContext
```

O backend também executa migrations pendentes ao iniciar, utilizando a URL direta.

## Render Free

O arquivo `render.yaml` define um Web Service Docker gratuito com health check em `/health`.
As conexões Neon são segredos (`sync: false`) e devem ser cadastradas no Render; o segredo JWT
é gerado pelo próprio Render. O serviço escuta a variável `PORT` automaticamente.

No plano gratuito, o Render suspende o serviço após um período sem tráfego. A primeira chamada
seguinte pode levar cerca de um minuto enquanto ocorre o cold start.
