# Integração do frontend com o backend

Este documento registra como validar a B-FE-03 contra a API ASP.NET Core de
`origin/main`. A implementação Flutter não altera o backend e não deve ser
validada contra dados de produção.

## Endpoints utilizados

As rotas abaixo são consumidas pelo `ApiClient` e pelo
`EspecialidadeRepositoryImpl`:

| Método | Rota | Autenticação | Uso |
| --- | --- | --- | --- |
| `GET` | `/api/especialidade` | Pública | Carrega o catálogo |
| `GET` | `/api/especialidade/{id}` | Pública | Consulta uma especialidade por GUID |
| `POST` | `/api/especialidade` | `Bearer`, perfil `Clinica` | Cadastra uma especialidade |
| `PUT` | `/api/profissional/especialidade` | `Bearer`, perfil `Profissional` | Define ou altera o vínculo |

Payloads enviados:

```json
{ "nome": "Fisioterapia Esportiva" }
```

```json
{ "especialidadeId": "00000000-0000-0000-0000-000000000001" }
```

O valor de `especialidadeId` é um GUID da API. Ele é obtido pela seleção de
um item do catálogo; nunca é solicitado como texto técnico ao usuário.

Resposta de catálogo:

```json
{ "id": "guid", "nome": "Fisioterapia Esportiva" }
```

O cadastro bem-sucedido deve responder HTTP 201 com a especialidade criada.
Conflitos de nome respondem HTTP 409. O backend normaliza espaços e compara
caixa/acentos conforme sua regra de catálogo.

## Autenticação e sessão

O serviço compartilhado de autenticação está preparado para:

- `POST /api/auth/login` com `{ "email": "...", "senha": "..." }`;
- `GET /api/auth/me`;
- `POST /api/auth/logout`.

O login real retorna `accessToken`, `expiresAtUtc` e `usuario`, contendo
`usuarioId`, `perfilId`, `email` e `tipoUsuario`. O token é armazenado pelo
`SecureSessionStore`; ele não é incluído em logs.

O `ApiClient` envia `Bearer` somente em chamadas protegidas e limpa a sessão
quando recebe HTTP 401. A tela de login ainda deve ser conectada pelo fluxo de
autenticação compartilhado do Grupo B. As rotas da B-FE-03 já usam
`RoleGate` para bloquear sessões ausentes ou com perfil incorreto.

## URL da API

O valor padrão local é:

```text
http://localhost:5048/api
```

Para configurar explicitamente:

```bash
flutter run -d chrome --web-port 5173 \
  --dart-define=API_BASE_URL=http://127.0.0.1:5048/api
```

Em outro dispositivo, `localhost` aponta para o próprio dispositivo. Nesse
caso, use o endereço acessível da máquina que executa a API, por exemplo:

```bash
--dart-define=API_BASE_URL=http://192.168.0.10:5048/api
```

Não coloque tokens, senhas ou connection strings no código ou no repositório.

## Banco de dados e migrations

O backend de `origin/main` está configurado para desenvolvimento com SQL
Server Express:

```text
.\SQLEXPRESS
Database: AgendaiFisioDb
Integrated Security: True
TrustServerCertificate: True
```

É necessário:

1. Disponibilizar uma instância SQL Server isolada para desenvolvimento.
2. Configurar uma connection string de desenvolvimento válida.
3. Aplicar as migrations do backend, incluindo
   `20261001120000_CriaCatalogoEspecialidadesEVinculo`.
4. Confirmar a existência da tabela de especialidades, do índice de nome e da
   coluna `Profissionais.EspecialidadeId`.

O `Program.cs` de `origin/main` não aplica migrations automaticamente. A
aplicação deve ser feita pelo procedimento oficial do backend, por exemplo:

```bash
dotnet ef database update \
  --project AgendaiFisio.csproj \
  --startup-project AgendaiFisio.csproj
```

Use uma connection string de desenvolvimento por variável de ambiente ou
user-secrets. Não use banco ou credenciais de produção.

## CORS para desenvolvimento

Para o Flutter Web executado em `http://localhost:5173`, o backend precisa
registrar uma política de desenvolvimento que permita explicitamente essa
origem, os métodos `GET`, `POST` e `PUT`, e os headers `Authorization` e
`Content-Type`. A política deve ser aplicada antes do mapeamento dos
controllers.

Este requisito ainda está pendente no backend de `origin/main`: a auditoria
não encontrou `AddCors`/`UseCors`, e o preflight local respondeu HTTP 405 sem
`Access-Control-Allow-Origin`. A correção deve ser feita pelo responsável do
backend, sem desabilitar a política de mesma origem de forma ampla.

## Perfis e contas de teste

São necessárias contas isoladas de desenvolvimento:

- uma conta `Profissional`, para login, seleção e alteração de especialidade;
- uma conta `Clinica`, para cadastro e teste de duplicidade;
- opcionalmente uma conta `Paciente`, para confirmar que as áreas protegidas
  permanecem bloqueadas.

O cadastro público aceita `Paciente` e `Profissional`. A conta `Clinica` é
provisionada pelo mecanismo administrativo do backend usando
`AdminSeed:Email` e `AdminSeed:Senha`, configurados fora do repositório.

Não registrar senhas no terminal compartilhado, nos testes ou neste documento.

## Validação ponta a ponta

1. Inicie o SQL Server de desenvolvimento e aplique as migrations.
2. Configure as contas de teste e inicie a API em `http://localhost:5048`.
3. Confirme `GET /openapi/v1.json` e o preflight CORS a partir de
   `http://localhost:5173`.
4. Inicie o Flutter:

   ```bash
   cd frontend
   flutter pub get
   flutter run -d chrome --web-port 5173 \
     --dart-define=API_BASE_URL=http://localhost:5048/api
   ```

5. Abra `/especialidades`, confirme a listagem, pesquisa, atualização manual,
   estado vazio e mensagens de erro.
6. Consulte um GUID existente em `/api/especialidade/{id}` e confirme o DTO
   `{id, nome}`.
7. Faça login como Profissional pelo fluxo compartilhado e abra
   `/profissional/especialidade`. Selecione um item, salve, confirme o sucesso,
   recarregue a sessão e verifique o vínculo no perfil profissional.
8. Faça login como Clínica e abra `/clinica/especialidades/nova`. Cadastre um
   nome válido e confirme HTTP 201, a mensagem de sucesso e a inclusão no
   catálogo local.
9. Repita o cadastro com o mesmo nome, variando caixa, acentos ou espaços, e
   confirme HTTP 409 sem inserir uma duplicata visual.
10. Teste uma sessão Paciente e uma sessão sem login nas duas rotas protegidas;
    ambas devem ser bloqueadas pela navegação e pela API.
11. Atualize a página e confirme que o catálogo continua vindo da API e que a
    sessão válida é recuperada do armazenamento seguro.

## Problemas conhecidos e pendências

- A integração ponta a ponta ainda depende de SQL Server de desenvolvimento;
  sem ele, os endpoints públicos falham ao acessar `.\SQLEXPRESS`.
- CORS ainda não está configurado no backend de `origin/main`.
- O frontend não cria uma tela de login própria: o login deve ser fornecido e
  conectado pelo fluxo compartilhado do Grupo B.
- O seletor está pronto como `EspecialidadeSelectorPage`, protegido pela rota
  `/profissional/especialidade`, e usa somente `especialidadeId` no repository.
  O responsável pelo perfil deve incorporá-lo à tela de perfil ou encaminhar
  para essa rota, mantendo o mesmo `EspecialidadeRepository` e a mesma sessão.
- O cadastro está protegido por `RoleGate` com `UserRole.clinica`; no backend,
  `Clinica` é o valor público normalizado a partir da constante interna
  `PerfilDeUsuario.Admin`.
- Não há fonte Inter licenciada ou fornecida nos assets deste repositório.
  O tema mantém `fontFamily: 'Inter'` como contrato visual, mas a entrega da
  fonte oficial em `assets/fonts/` ainda depende do Grupo B. Até lá, o sistema
  operacional usará fallback; nenhuma fonte foi baixada ou adicionada sem
  licença verificável.
- Não existe rota de remoção de especialidade, portanto a UI não oferece
  remoção.

## Verificações locais do frontend

Executar na pasta `frontend/`:

```bash
dart format .
flutter analyze
flutter test
flutter build web
```

Os testes cobrem DTOs, requests do repository, GUID, 401/400/403/404/409,
duplicidade, estados, restrição por perfil e layout responsivo. A execução
contra a API real depende dos requisitos de ambiente descritos acima.
