# Publicação no MonsterASP.NET

O backend usa ASP.NET Core 10, IIS/InProcess e SQL Server. A publicação não usa Docker.

## Banco

O banco do MonsterASP.NET pode começar vazio. O schema completo está na migration inicial em
`AgendaiFisio.Repositorio/Migrations/SqlServer`.

No painel do banco, copie a connection string de **Local access**. Ela deve ser usada pelo website
porque a API e o SQL Server estão dentro da rede do Monster. Não grave a senha no Git.

## Variáveis do website

Cadastre em `Website > Scripting > Environment variables`:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Padrao=<connection string Local access do MSSQL>
JwtSettings__SecretKey=<chave forte com pelo menos 32 caracteres>
JwtSettings__Issuer=AgendaiFisioAPI
JwtSettings__Audience=AgendaiFisioClients
JwtSettings__ExpirationHours=8
```

Reinicie o website depois de alterar variáveis. A API aplica migrations pendentes ao iniciar. No
MVP, as contas são criadas somente pelo cadastro público como `Paciente` ou `Profissional`.

## Git deploy

1. Abra `Website > Git deploy`.
2. Informe `https://github.com/Alunos-ADS-UNIP/AgendaiFisio.git`.
3. Selecione a branch `main`.
4. Escolha `Build .NET app`.
5. Selecione `AgendaiFisio.Api/AgendaiFisio.Api.csproj`.
6. Execute `Pull latest` e depois `Build & publish`.
7. Confirme `GET /health`, login e uma rota protegida.

## Aplicação Razor

`AgendaiFisio.Web` é apenas o esqueleto compilável solicitado. Quando as views Razor forem
implementadas, publique-o em um segundo website do Monster. API e Web podem usar o mesmo banco,
mas cada website deve ter suas próprias variáveis de ambiente.

## Desenvolvimento externo

Para aplicar migrations a partir do computador, habilite temporariamente o acesso remoto do banco,
use a connection string **Remote access** em `ConnectionStrings__Padrao` e desabilite o acesso
novamente ao terminar.
