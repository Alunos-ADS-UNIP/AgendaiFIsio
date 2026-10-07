# Relatório de auditoria técnica — AgendaiFisio

**Data:** 07/10/2026
**Escopo:** estado local do repositório em `/Users/joaoschiavoni/Myprojects/AgendaiFIsio`
**Commit base:** `3caa849` (`main`, alinhado a `origin/main`)
**Conclusão:** o backend é um MVP funcional e a regressão principal está saudável. Para o escopo confirmado — desenvolvimento e apresentação local, sem publicação em produção — ele está em condição de demonstração. Há limitações relevantes para uma eventual evolução a produção, especialmente migrations, configuração operacional, ciclo de agendamento e ausência de uma suíte integrada ao `dotnet test`/CI.

## 1. Resumo executivo

O projeto é uma API ASP.NET Core em .NET 10, com Entity Framework Core, SQL Server, autenticação JWT e documentação OpenAPI/Scalar em Development. Há cinco áreas expostas pela API: autenticação, pacientes, profissionais, especialidades e agendamentos.

O que está em bom estado:

- Build Release, publicação e verificação de formatação passam sem warnings ou erros.
- A migration local representa o modelo atual e o EF não detecta mudanças pendentes no modelo.
- A regressão ponta a ponta existente passou em **112 verificações**.
- Autorização por perfil, isolamento de agenda, validação de CPF, catálogo de especialidades, paginação do histórico, normalização UTC e prevenção sequencial de conflito foram exercitados com sucesso.
- O NuGet não encontrou pacotes vulneráveis nem obsoletos/deprecated nas fontes consultadas.
- Senhas são armazenadas com BCrypt e o autocadastro não permite criar o papel administrativo `Clinica`.

Principais limitações:

1. O histórico de migrations foi substituído localmente por uma nova `InitialCreate`; isso quebra a atualização de bancos que já receberam as migrations anteriores, embora não impeça uma demonstração com banco recriado.
2. A aplicação Production inicia sem validar configurações obrigatórias e responde HTTP 500 na primeira rota protegida quando o JWT não está configurado. Isso não afeta a apresentação em Development já configurada.
3. O ciclo de agendamento está incompleto e o cliente pode escolher estados que deveriam ser controlados pelo servidor.
4. `dotnet test` não executa testes: a regressão é um console app manual, dependente de API e SQL Server externos.
5. As credenciais locais estão versionadas. Para este projeto exclusivamente demonstrativo, a senha do SQL Server foi classificada como risco aceito, desde que não seja reutilizada em outro ambiente.

## 2. Estado do repositório auditado

O worktree já estava alterado antes da auditoria. As mudanças foram preservadas:

- 11 arquivos antigos de migration aparecem removidos.
- `Migrations/20261006225916_InitialCreate.cs` e seu designer aparecem como novos.
- `appsettings.Development.json` foi alterado para usar SQL Server em `localhost:1433`.
- Há arquivos `.DS_Store` não rastreados.

Este relatório analisa esse estado local, não apenas o conteúdo do último commit.

## 3. Arquitetura e funcionalidades existentes

### Plataforma

- ASP.NET Core Web API, `net10.0`.
- Entity Framework Core 10 + SQL Server.
- JWT Bearer com chave simétrica HMAC SHA-256.
- BCrypt para hash de senha.
- OpenAPI + Scalar apenas em Development.
- 2.303 linhas nos diretórios principais de código C# (sem migrations/testes/artefatos).

### Domínio persistido

- `Usuario`
- `Paciente` e `Endereco`
- `Profissional`
- `Especialidade`
- `Agendamento`
- `AvaliacaoFisioterapeuta`
- `PlanoTerapeutico`

`AvaliacaoFisioterapeuta` e `PlanoTerapeutico` existem no modelo e no banco, mas não possuem serviços nem endpoints.

### Endpoints disponíveis

| Área | Método e rota | Acesso | Situação |
|---|---|---|---|
| Auth | `POST /api/auth/register` | Público | Cria Paciente ou Profissional |
| Auth | `POST /api/auth/login` | Público | Emite JWT |
| Paciente | `PUT /api/paciente/completar-perfil` | Paciente | Atualiza próprio perfil |
| Paciente | `GET /api/paciente/{id}/historico-consultas` | Paciente/Profissional vinculado | Paginado |
| Profissional | `GET /api/profissional` | Autenticado | Lista paginada e filtrada |
| Profissional | `GET /api/profissional/{id}` | Autenticado | Detalhe público do perfil |
| Profissional | `PUT /api/profissional/completar-perfil` | Profissional | Atualiza próprio perfil |
| Profissional | `PUT /api/profissional/especialidade` | Profissional | Define/troca especialidade |
| Especialidade | `GET /api/especialidade` | Público | Lista catálogo |
| Especialidade | `GET /api/especialidade/{id}` | Público | Obtém item |
| Especialidade | `POST /api/especialidade` | Clínica | Cria item |
| Agendamento | `GET /api/agendamento` | Autenticado | Lista conforme identidade |
| Agendamento | `GET /api/agendamento/{id}` | Autenticado | Detalhe conforme identidade |
| Agendamento | `POST /api/agendamento` | Paciente/Clínica | Cria agendamento |

## 4. Testes e verificações executados

| Verificação | Resultado |
|---|---|
| `dotnet restore` | Passou |
| `dotnet build -c Release --no-restore` | Passou, 0 warnings e 0 erros |
| `dotnet format --verify-no-changes --severity info` | Passou |
| Build do projeto `Tests/Regression` | Passou, 0 warnings e 0 erros |
| `dotnet test -c Release` | Sai com sucesso, mas não descobre/executa testes |
| `dotnet ef migrations list` com SQL Server | Migration local reconhecida/aplicada |
| `dotnet ef migrations has-pending-model-changes` | Nenhuma mudança pendente no modelo |
| Regressão HTTP + SQL Server | **112/112 verificações aprovadas** |
| `dotnet list package --vulnerable --include-transitive` | Nenhuma vulnerabilidade conhecida |
| `dotnet list package --deprecated --include-transitive` | Nenhum pacote deprecated |
| `dotnet list package --outdated --include-transitive` | Há atualizações, ver seção 8 |
| `dotnet publish -c Release` | Passou; 40 arquivos publicados |
| Smoke test Production sem configurações externas | Processo inicia; rota protegida retorna **HTTP 500** |

### Cobertura funcional da regressão existente

A regressão aprovada verifica, entre outros pontos:

- conexão com banco e ausência de migrations pendentes;
- CPF válido/inválido, com e sem pontuação;
- exigência de offset explícito e normalização de horários para UTC;
- OpenAPI em Development;
- rejeição de acesso sem token e de papéis indevidos;
- cadastro e login de Paciente, Profissional e Clínica provisionada;
- catálogo público e criação administrativa de especialidades;
- normalização e rejeição de especialidade duplicada;
- atualização e isolamento dos perfis;
- não exposição de CPF/data de nascimento no detalhe do profissional;
- autorização do histórico sem revelar existência de paciente inacessível;
- paginação, limite de 50, ordenação e datas UTC no histórico;
- isolamento da agenda por paciente/profissional;
- criação administrativa e pelo próprio paciente;
- rejeição de horário passado, status inválido e profissional inativo/inexistente;
- conflito sequencial de horário e liberação de horário cancelado;
- limpeza dos dados de teste.

### Limitações dos testes realizados

- Não foi feito teste de carga, stress, pentest ou DAST.
- Não há cobertura calculada por linha/ramo.
- Não foi exercitada concorrência real de múltiplos requests simultâneos.
- Não foi validado deploy em nuvem, proxy reverso ou ambiente Windows/Linux externo.
- Avaliação fisioterapêutica e plano terapêutico não têm API para testar.

## 5. Achados priorizados

### RISCO ACEITO-01 — Credenciais do ambiente local versionadas

**Evidência:** `appsettings.Development.json` contém a senha do usuário `sa` na connection string e uma chave JWT estática. O arquivo é rastreado pelo Git.

**Contexto confirmado:** o projeto será usado somente em desenvolvimento e para apresentação, sem deploy em produção. A senha do SQL Server é exclusiva desse ambiente demonstrativo e, nesse contexto, **não precisa ser rotacionada**.

**Risco residual:** qualquer pessoa com acesso ao repositório conhece a credencial do banco local e a chave JWT da demonstração. Isso só se torna relevante se esses valores forem reutilizados, se o banco/API forem expostos em rede ou se o projeto mudar de finalidade.

**Recomendação:**

1. Manter a senha exclusiva da demonstração e não reutilizá-la em contas, bancos ou serviços reais.
2. Evitar expor a porta 1433 e a API fora da máquina/rede controlada durante a apresentação.
3. Se futuramente houver deploy ou compartilhamento público, mover os valores para variáveis de ambiente/user-secrets e gerar novas credenciais antes disso.

### CRÍTICO-02 — Histórico de migrations reescrito

**Evidência:** seis migrations rastreadas (mais designers) aparecem removidas e foram substituídas localmente por `20261006225916_InitialCreate`.

**Risco:** um banco já atualizado pelas migrations antigas não conhece a nova ID. Na próxima atualização, o EF tentará executar a `InitialCreate` e criar tabelas já existentes, causando falha. Recriar o banco para contornar isso também pode destruir dados de pacientes e histórico clínico.

**Recomendação:** restaurar o histórico de migrations publicado e criar uma migration incremental nova. Se o squash for intencional, formalizar uma baseline com script idempotente e plano explícito de migração por ambiente; nunca aplicar a nova `InitialCreate` diretamente em banco existente.

### ALTO-01 — Production inicia “saudável” sem configurações obrigatórias e falha no primeiro request

**Evidência reproduzida:** com `ASPNETCORE_ENVIRONMENT=Production` e sem `JwtSettings`, o Kestrel iniciou em `http://localhost:5000`. `GET /api/profissional` retornou HTTP 500. O log registrou `ArgumentNullException` em `Encoding.GetBytes(secretKey)` (`Program.cs`, configuração JWT).

**Risco:** deploy/health superficial pode marcar a instância como pronta mesmo estando inutilizável. A connection string também só falhará quando o DbContext for efetivamente usado.

**Recomendação:** tipar e validar configurações no startup com `ValidateDataAnnotations()` e `ValidateOnStart()`, exigir chave com entropia/tamanho adequados e validar connection string. Adicionar `/health/live` e `/health/ready`, com readiness verificando banco e configurações essenciais.

### ALTO-02 — Fluxo administrativo inicial não permite cumprir a própria orientação de segurança

**Evidência:** `Program.cs` orienta trocar a senha após o primeiro login, mas não existe endpoint de troca de senha. Se o e-mail já existir, o seed não atualiza o hash.

**Risco:** a credencial inicial tende a permanecer indefinidamente, muitas vezes em variável de ambiente, script ou histórico operacional.

**Recomendação:** implementar troca obrigatória no primeiro login ou usar um provedor de identidade/ASP.NET Core Identity. Remover o seed após bootstrap e registrar auditoria de criação/rotação.

### ALTO-03 — Ciclo de agendamento incompleto e status controlado pelo cliente

**Evidência:** só existem GET e POST. Não há cancelar, confirmar, concluir, remarcar ou atualizar observações. O POST aceita `Agendado`, `Confirmado`, `Cancelado` e `Concluido`; portanto um paciente pode criar diretamente um atendimento futuro já concluído ou cancelado.

**Risco:** estados clinicamente/operacionalmente inválidos e impossibilidade de operar a agenda após a criação.

**Recomendação:** no POST, o servidor deve definir `Agendado`. Criar transições autorizadas explícitas (`PATCH`/ações), com máquina de estados, auditoria, controle de concorrência e políticas por papel. Definir cancelamento, no-show, confirmação, conclusão e remarcação.

### ALTO-04 — Conflito considera apenas o mesmo instante, não a duração

**Evidência:** a regra e o índice único usam somente `(ProfissionalId, DataHora)`.

**Risco:** consultas às 10:00 e 10:15 são aceitas mesmo que ambas durem 60 minutos.

**Recomendação:** modelar duração ou `DataHoraFim`, disponibilidade/intervalos e detectar sobreposição. Definir timezone da unidade, horários de trabalho, bloqueios, feriados e antecedência mínima/máxima.

### ALTO-05 — Testes não fazem parte de uma suíte automatizada

**Evidência:** `Tests/Regression` é `OutputType=Exe`, sem xUnit/NUnit/MSTest. O projeto principal remove `Tests/**/*.cs`. `dotnet test` apenas compila e termina sem executar casos. Não há pipeline CI no repositório.

**Risco:** um CI ingênuo pode ficar verde com zero testes; a regressão depende de API, SQL Server, admin seed e comandos manuais.

**Recomendação:** criar projetos de testes descobertos por `dotnet test`, separar unitários, integração com `WebApplicationFactory` e banco descartável/Testcontainers, e manter a regressão E2E como camada adicional. Falhar CI quando nenhum teste for descoberto e publicar cobertura.

### ALTO-06 — Integridade e validação de dados pessoais insuficientes

**Evidências:**

- CPF não tem índice único em Paciente nem Profissional e não é normalizado de forma uniforme.
- Data de nascimento aceita `DateTime` padrão, futuro e idades impossíveis.
- `PacienteUpdateDTO` não limita tamanhos nem valida telefone, CEP, UF, nome e outros campos.
- Muitas colunas viram `nvarchar(max)`.
- Não há regra clara para impedir o mesmo CPF nas duas categorias.

**Risco:** duplicidade de pessoa, cadastros inconsistentes, dados impossíveis e crescimento desnecessário das linhas/índices.

**Recomendação:** normalizar documentos antes de persistir, decidir unicidade global ou por perfil, adicionar constraints/índices, validadores de domínio e limites coerentes no DTO e no modelo EF.

### MÉDIO-01 — Corrida no duplo agendamento vira HTTP 500

**Evidência:** o serviço faz `AnyAsync` e depois `SaveChangesAsync`. O índice único protege o banco, mas `DbUpdateException` por requests concorrentes não é traduzida; o controller só trata `InvalidOperationException` como 409.

**Risco:** integridade é preservada, porém o segundo request concorrente recebe erro interno em vez de conflito de negócio.

**Recomendação:** capturar apenas os códigos SQL de violação do índice de agenda e devolver 409; adicionar teste simultâneo real.

### MÉDIO-02 — Erros internos podem ser expostos ao cliente

**Evidência:** registro retorna `ex.Message` para qualquer exceção. Login retorna HTTP 500 com `Detalhe = ex.Message`.

**Risco:** mensagens de EF/SQL/configuração podem revelar estrutura interna e informações úteis para ataque.

**Recomendação:** usar middleware global de exceções/`ProblemDetails`, mensagens públicas estáveis, correlation ID e log interno com stack trace. Capturar exceções de domínio específicas nos controllers.

### MÉDIO-03 — Política de acesso ao histórico precisa de validação LGPD/produto

**Evidência:** qualquer agendamento não cancelado, inclusive futuro ou antigo, libera ao profissional o histórico passado completo do paciente, incluindo atendimentos de outros profissionais.

**Risco:** acesso mais amplo e duradouro que o necessário para a finalidade do atendimento.

**Recomendação:** confirmar base legal/consentimento, limitar janela e vínculo ativo, registrar auditoria de acesso e avaliar segmentação do histórico. A regra atual está documentada e testada, mas isso não substitui a decisão de privacidade.

### MÉDIO-04 — Campos de profissional têm defaults problemáticos

**Evidência:** no cadastro, `DataCadastro` não é preenchida e permanece `0001-01-01`. No update, `Ativo` é `bool` não anulável; se um cliente omitir o campo, o model binding usa `false` e pode desativar o profissional involuntariamente.

**Recomendação:** preencher `DataCadastro = DateTime.UtcNow` no servidor; retirar `Ativo` do update comum ou torná-lo uma ação administrativa explícita/patch com nullable.

### MÉDIO-05 — Endurecimento de autenticação e transporte ausente

**Evidências:** senha mínima de apenas seis caracteres; sem rate limiting, lockout, MFA, recuperação/troca de senha, revogação/refresh, auditoria de login ou versionamento de token. Em execução HTTP local, `UseHttpsRedirection` emite aviso porque não consegue determinar porta HTTPS. Não há HSTS configurado.

**Recomendação:** adotar ASP.NET Core Identity ou política equivalente, rate limiting por IP/conta, logs de segurança, rotação/revogação e HTTPS/HSTS definidos por ambiente/proxy.

### MÉDIO-06 — Consultas com risco de escala

**Evidências:** lista de agendamentos não possui paginação/limite; filtro de nome usa `ToLower().Contains`, o que dificulta uso eficiente de índice; cancelamento e histórico dependem de strings livres de status no banco.

**Recomendação:** paginar agenda, adicionar índices guiados pelas consultas, usar collation/`LIKE` apropriadamente, modelar status como enum/conversão restrita e medir planos de execução com volume real.

### BAIXO-01 — Documentação e experiência de desenvolvimento incompletas

**Evidências:** não há README, solução `.sln`, Docker Compose, exemplo de variáveis, instrução única para migrations/testes, CI ou documentação de deploy. `AgendaiFisio.http` ainda chama `/weatherforecast`, rota inexistente. `.gitignore` não ignora `.DS_Store`.

**Recomendação:** criar guia de setup/teste/deploy, compose de desenvolvimento, arquivo `.env.example` sem segredo, atualizar o `.http`, ignorar arquivos do macOS e documentar decisões de migrations.

## 6. Funcionalidades que ainda faltam

### Essenciais para o produto de agenda

- Cancelar, confirmar, concluir e remarcar agendamento.
- Duração da consulta e prevenção de sobreposição.
- Agenda/grade de disponibilidade por profissional.
- Horário de trabalho, intervalos, bloqueios, férias e feriados.
- Regras de antecedência e timezone da clínica.
- Paginação da agenda e busca por intervalo de datas.
- Auditoria das mudanças de status e responsável por cada ação.
- Notificações/lembretes e confirmação do paciente, caso façam parte do produto.

### Fluxos de usuário

- Consultar o próprio perfil de paciente (há update, mas não GET correspondente).
- Trocar/recuperar senha e validar e-mail.
- Administração de usuários, ativação/desativação e papéis.
- Política de exclusão/anonimização e retenção de dados LGPD.
- Eventual exclusão/edição controlada de especialidades.

### Funcionalidade clínica

- CRUD e autorização de avaliação fisioterapêutica.
- CRUD e acompanhamento de plano terapêutico.
- Evolução/sessão/prontuário, anexos e consentimentos, se previstos no escopo.
- Regra clara de quais profissionais acessam quais dados e por quanto tempo.

### Operação e qualidade

- Health/readiness checks.
- Observabilidade: logs estruturados, métricas, traces e correlation ID.
- CI/CD, testes descobertos automaticamente e cobertura.
- Estratégia de backup/restauração e migrations por ambiente.
- Containers/configuração reproduzível e documentação de deploy.
- CORS configurado caso o frontend esteja em outra origem.

## 7. Qualidade do código

### Pontos positivos

- Separação razoável entre controllers, services, DTOs, entities e validations.
- Uso consistente de `AsNoTracking` em várias consultas somente leitura.
- DTOs evitam expor CPF e nascimento no detalhe de profissional.
- Índices protegem e aceleram casos importantes: e-mail, CREFITO, especialidade, agenda e histórico.
- O filtro de índice permite reutilizar horário cancelado.
- A criação de especialidade trata corretamente a corrida de duplicidade usando códigos SQL específicos.
- A regressão limpa seus próprios dados em `finally`.
- Datas de criação de agendamento exigem offset e são persistidas em UTC.

### Dívida técnica

- Muitos `using` redundantes e comentários excessivos em relação ao volume de código.
- Controllers misturam traduções de erro inconsistentes (400/404/409/500 e formatos variados).
- Strings de papel e status aparecem espalhadas.
- Interfaces/métodos retornam `bool` sem necessidade (`UpdatePacienteAsync` sempre retorna `true`).
- Falta `CancellationToken` nas operações assíncronas HTTP/banco.
- DTOs antigos não usados (`PacienteCreateDTO`, `PacienteResponseDTO`, `ProfissionalCreateDTO`, `ProfissionalResponseDTO`) aumentam ambiguidade.

## 8. Dependências

Nenhuma vulnerabilidade/depreciação foi reportada, mas há atualizações disponíveis:

- `Microsoft.AspNetCore.Authentication.JwtBearer`: 10.0.11 → 10.0.12.
- `Microsoft.AspNetCore.OpenApi`: 10.0.9 → 10.0.12.
- `Scalar.AspNetCore`: 2.16.20 → 2.17.14.
- `Microsoft.OpenApi`: 2.7.5 → 3.10.2 (major; exige validação de compatibilidade).
- Diversas transitivas também possuem versões novas; devem ser atualizadas por meio dos pacotes de topo, não fixadas aleatoriamente.

Recomenda-se alinhar os pacotes Microsoft no mesmo patch 10.0.12, executar a regressão completa e avaliar a migração major de `Microsoft.OpenApi` separadamente.

## 9. Plano recomendado

### Antes de qualquer deploy futuro

1. Substituir as credenciais demonstrativas e removê-las da configuração versionada.
2. Resolver a estratégia de migrations sem apagar o histórico publicado.
3. Validar JWT e banco no startup; adicionar readiness real.
4. Bloquear status arbitrário no POST de agendamento.
5. Corrigir vazamento de detalhes de exceção.
6. Criar testes automatizados descobertos por `dotnet test` e CI obrigatório.

### Próximo incremento funcional

1. Definir máquina de estados e permissões do agendamento.
2. Modelar duração, disponibilidade e sobreposição.
3. Implementar cancelamento/remarcação/confirmação/conclusão.
4. Corrigir validação/unicidade/normalização de dados pessoais.
5. Validar formalmente a política de acesso ao histórico.

### Depois

1. Implementar avaliações e planos terapêuticos ou remover as entidades prematuras do escopo ativo.
2. Adicionar observabilidade, backup, LGPD e documentação operacional.
3. Fazer teste de carga e segurança antes do go-live.

## 10. Veredito

**Estado atual: MVP backend funcional e adequado para apresentação local em Development; não avaliado como pronto para produção.**

A base tem boas decisões de autorização e integridade em vários fluxos e a regressão de 112 checks oferece confiança real no comportamento atual. Para a apresentação, a credencial local versionada foi aceita conscientemente e não exige rotação. Reescrita de migrations, falha tardia de configuração Production e lacunas centrais no agendamento continuam sendo pontos importantes apenas se o projeto evoluir além da demonstração atual.
