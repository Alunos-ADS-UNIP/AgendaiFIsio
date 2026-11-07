# AgendaiFisio

Solution organizada no padrão em camadas usado pelo template do PIM:

```text
AgendaiFisio.Web ──┐
                   ├──> AgendaiFisio.Negocio ──> AgendaiFisio.Repositorio ──> AgendaiFisio.Modelos
AgendaiFisio.Api ──┘
```

- `AgendaiFisio.Modelos`: entidades, DTOs, constantes e validações.
- `AgendaiFisio.Repositorio`: DbContext, repositories, unidade de trabalho e migrations SQL Server.
- `AgendaiFisio.Negocio`: regras de negócio e contratos consumidos pelos controllers.
- `AgendaiFisio.Api`: endpoints REST usados pelo Flutter, JWT, OpenAPI e Scalar.
- `AgendaiFisio.Web`: esqueleto reservado para ASP.NET Core MVC com views Razor.

## Compilar

```bash
dotnet restore AgendaiFisio.sln
dotnet build AgendaiFisio.sln
```

## Banco local

Configure `ConnectionStrings:Padrao` em `AgendaiFisio.Api/appsettings.Development.json` ou pela
variável `ConnectionStrings__Padrao` e execute:

```bash
dotnet tool restore
dotnet ef database update \
  --project AgendaiFisio.Repositorio/AgendaiFisio.Repositorio.csproj \
  --startup-project AgendaiFisio.Api/AgendaiFisio.Api.csproj
```

## Executar a API

```bash
dotnet run --project AgendaiFisio.Api/AgendaiFisio.Api.csproj
```

O procedimento de hospedagem está em `docs/monsteraspnet.md`.
