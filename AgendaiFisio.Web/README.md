# AgendaiFisio.Web

Portal ASP.NET Core MVC com Razor Views para pacientes e profissionais. O projeto reutiliza a identidade visual do aplicativo Flutter e consome exclusivamente a API do AgendaiFisio; ele não acessa o banco de dados diretamente.

## Executar localmente

Com a API disponível em `http://localhost:5048`:

```bash
dotnet run --project AgendaiFisio.Web
```

Para apontar para outro backend:

```bash
Backend__BaseUrl=https://sua-api.exemplo.com dotnet run --project AgendaiFisio.Web
```

No MonsterASP.NET, cadastre `Backend__BaseUrl` como variável de ambiente com a URL pública HTTPS da API.

## Fluxos implementados

- página pública, login, cadastro, aceite LGPD e consentimento do responsável para menores de 16 anos;
- sessão web protegida por cookie, com o JWT mantido somente na sessão do servidor;
- dashboard separado para paciente e profissional;
- busca e perfil público de profissionais;
- criação, listagem, detalhe, reagendamento e cancelamento de consultas;
- perfil completo do paciente e do profissional, endereço por CEP e foto profissional;
- histórico do paciente;
- busca de pacientes pelo profissional;
- bloqueio de dias e horários da agenda profissional.

As rotas protegidas respeitam os papéis `Paciente` e `Profissional` devolvidos pelo backend.
