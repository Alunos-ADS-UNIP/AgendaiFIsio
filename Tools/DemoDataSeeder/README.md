# Carga de demonstração

Esta ferramenta remove os dados operacionais e as contas do banco informado, preserva o
schema, o histórico de migrations e as 16 especialidades, e cria três profissionais por
especialidade. As fotos são enviadas pela API autenticada para que o próprio backend grave
os arquivos no S3 configurado no MonsterASP.NET.

## Proteções

- A execução normal exige `AGENDAIFISIO_SEED_CONFIRM=APAGAR_E_RECRIAR_DADOS`.
- A conexão deve ser informada por variável de ambiente ou pelo arquivo local ignorado pelo Git
  `Tools/DemoDataSeeder/connection-string.local.txt`.
- A API e as migrations são validadas antes da exclusão.
- Uma conta temporária valida cadastro, login, JWT, upload e remoção no S3 antes da exclusão.
- A exclusão e a inserção no SQL Server são realizadas na mesma transação.
- `--validate-only` valida os 48 perfis e as seis imagens sem acessar banco, API ou S3.

## Executar

Salve somente a connection string completa no arquivo local e, na raiz do repositório, execute:

```bash
AGENDAIFISIO_SEED_CONFIRM=APAGAR_E_RECRIAR_DADOS \
dotnet run --project Tools/DemoDataSeeder/DemoDataSeeder.csproj
```

A URL padrão é `http://agendaifisio.runasp.net`. Para outro ambiente, configure
`AGENDAIFISIO_SEED_API_URL`.

Ao concluir, a ferramenta grava os acessos fictícios em
`output/credenciais-terapeutas.csv`. Todas as contas usam a senha de demonstração registrada
nesse arquivo.
