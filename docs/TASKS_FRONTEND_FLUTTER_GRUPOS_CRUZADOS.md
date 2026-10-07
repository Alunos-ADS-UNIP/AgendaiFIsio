# Tasks de Frontend Flutter - Grupos Cruzados

Todas as tasks devem seguir o [Design System Flutter](./DESIGN_SYSTEM_FLUTTER.md).

## Regra de distribuição

- O Grupo A implementa no Flutter as funcionalidades de Pacientes e Agendamentos feitas pelo Grupo B.
- O Grupo B implementa no Flutter as funcionalidades de Fisioterapeutas, Especialidades e Disponibilidade feitas pelo Grupo A.
- As tasks abaixo distinguem rotas disponíveis hoje de funcionalidades descritas no escopo original que ainda dependem de backend.

## Base compartilhada - executar primeiro

### FE-COMUM-01 - Sessão, autenticação e cliente HTTP

**Responsáveis:** um integrante de cada grupo, em uma única implementação compartilhada.

**Rotas:**

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/logout`

**Implementação Flutter:**

- Criar cliente HTTP único usando a biblioteca já adotada pelo aplicativo.
- Salvar `accessToken`, `usuarioId`, `perfilId`, `email`, `tipoUsuario` e `expiresAtUtc` na sessão.
- Guardar o token em armazenamento seguro; não usar preferências comuns para a senha ou o JWT.
- Adicionar `Authorization: Bearer <token>` automaticamente nas chamadas protegidas.
- Não adicionar o header em login, cadastro ou leitura do OpenAPI.
- Limpar a sessão em logout e em respostas `401`.
- Criar guardas de navegação para `Paciente`, `Profissional` e `Clinica`.
- Não registrar JWT, senha ou CPF nos logs.

**Critérios de aceite:**

- Após o login, uma chamada a `/api/auth/me` funciona sem copiar o JWT manualmente.
- Reiniciar o aplicativo restaura uma sessão ainda válida.
- `401` leva à tela de login e limpa os dados locais.
- Uma função de Clínica não aparece nem fica acessível para Paciente ou Profissional.

---

## Grupo A - frontend do backend do Grupo B

### A-FE-01 - Área "Meu perfil" do paciente

**Responsável:** Dudu.

**Origem invertida:** cadastro/consulta de pacientes de Jonas e atualização de paciente de Joao V.

**Rotas disponíveis:**

- `POST /api/auth/register`, com `tipoUsuario: "Paciente"`
- `GET /api/auth/me`
- `GET /api/paciente/me`
- `PUT /api/paciente/completar-perfil`

**Implementação Flutter:**

- Criar tela de cadastro de conta de paciente.
- Criar tela de visualização do próprio perfil.
- Criar formulário de completar/editar perfil com dados pessoais e endereço.
- Usar `/api/paciente/me`; nunca pedir ou montar manualmente o ID do paciente.
- Aplicar máscaras de CPF, telefone e CEP somente na apresentação, enviando o formato aceito pela API.
- Exibir validações retornadas pela API por campo e estados de carregamento, sucesso e erro.

**Critérios de aceite:**

- Paciente cadastrado consegue entrar, consultar e atualizar o próprio perfil.
- O aplicativo não envia `pacienteId` em nenhuma chamada de perfil próprio.
- Campos opcionais omitidos não apagam valores existentes.
- Profissional e Clínica não acessam essa tela.

### A-FE-02 - Agenda do paciente e novo agendamento

**Responsável:** Liska.

**Origem invertida:** criação e consulta de agendamentos de Davi.

**Rotas disponíveis:**

- `GET /api/agendamento?data=&profissionalId=&status=`
- `GET /api/agendamento/{id}`
- `POST /api/agendamento`
- `GET /api/profissional`
- `GET /api/profissional/{id}`
- `GET /api/especialidade`

**Implementação Flutter:**

- Criar tela "Meus agendamentos" com filtros por data, profissional e status.
- Criar detalhe do agendamento com profissional, data/hora, status e observações.
- Criar fluxo de novo agendamento: selecionar especialidade, profissional, data/hora e observações.
- O corpo do `POST /api/agendamento` deve conter somente `profissionalId`, `dataHora`, `status` opcional e `observacoes` opcional.
- Converter a data selecionada para ISO 8601 com offset ou `Z` e mostrar ao usuário no fuso local.
- Tratar `404` para profissional indisponível/inexistente e `409` para conflito de horário.

**Critérios de aceite:**

- Paciente cria agendamento sem enviar o próprio ID.
- Após `201`, o novo agendamento aparece na listagem e abre no detalhe.
- Filtros podem ser combinados e limpos.
- Um conflito de horário não duplica o registro e apresenta mensagem compreensível.

### A-FE-03 - Histórico do paciente e consulta pelo fisioterapeuta

**Responsável:** Caio.

**Origem invertida:** histórico de Joao V e ciclo do atendimento de Gabriel.

**Rotas disponíveis:**

- `GET /api/paciente/me/historico-consultas?pagina=&tamanhoPagina=`
- `GET /api/paciente/{pacienteId}/historico-consultas?pagina=&tamanhoPagina=` - somente Profissional autorizado

**Implementação Flutter:**

- Criar histórico do próprio paciente com paginação ou carregamento incremental.
- Mostrar data/hora, profissional e status de cada consulta.
- Criar componente reutilizável de histórico para a visão do fisioterapeuta.
- Na visão de Paciente, usar exclusivamente a rota `/me/historico-consultas`.
- Tratar histórico vazio, fim da paginação, `403` e `404` sem revelar se outro paciente existe.

**Critérios de aceite:**

- Paciente vê apenas o próprio histórico.
- Profissional só abre o histórico explícito quando a API autorizar o vínculo.
- A ordenação permanece da consulta mais recente para a mais antiga.
- A tela não duplica itens ao carregar páginas adicionais.

### A-FE-04 - Clínica agenda para um paciente

**Responsável:** Caio.

**Prioridade:** baixa para a apresentação; fluxo administrativo separado.

**Rota disponível:**

- `POST /api/agendamento/administrativo`

**Implementação Flutter:**

- Criar formulário administrativo que recebe `pacienteId`, `profissionalId`, `dataHora`, `status` e `observacoes`.
- Exibir a opção somente para `tipoUsuario == "Clinica"`.
- Não reutilizar esse contrato no fluxo comum do paciente.

**Critérios de aceite:**

- Paciente e Profissional não visualizam nem acessam o fluxo.
- Clínica recebe mensagens adequadas para paciente/profissional inexistente e conflito de horário.

### A-FE-05 - Gestão administrativa de pacientes

**Responsável:** Dudu.

**Status:** bloqueada por backend.

**Escopo do PDF ainda sem rotas:** listagem paginada de pacientes, busca por nome/documento e detalhe administrativo de um paciente.

**Dependência para iniciar integração:** o backend precisa publicar contratos de listagem e detalhe com autorização de Clínica. A UI e os testes de widget podem ser preparados com mocks, mas a task não deve ser considerada concluída sem integração real.

### A-FE-06 - Reagendamento, cancelamento e mudança de status

**Responsável:** Caio.

**Status:** bloqueada por backend.

**Escopo do PDF ainda sem rotas:** reagendar, cancelar e alterar status para Confirmado, Em andamento, Concluído ou Falta.

**Dependência para iniciar integração:** definir endpoints, papéis autorizados, transições válidas e tratamento de concorrência. Não simular sucesso somente no estado local do Flutter.

---

## Grupo B - frontend do backend do Grupo A

### B-FE-01 - Catálogo de fisioterapeutas

**Responsável:** Jonas.

**Origem invertida:** listagem e detalhe de fisioterapeutas de Dudu.

**Rotas disponíveis:**

- `GET /api/profissional?nome=&especialidadeId=&ativo=&pagina=&tamanhoPagina=`
- `GET /api/profissional/{id}`
- `GET /api/especialidade`

**Implementação Flutter:**

- Criar listagem paginada de fisioterapeutas.
- Criar busca por nome, filtro por especialidade e filtro por ativo/inativo.
- Criar tela de detalhe com nome, CREFITO, bio e especialidade devolvida pela API.
- Usar debounce na busca por nome e preservar os filtros ao voltar do detalhe.

**Critérios de aceite:**

- A listagem não carrega todas as páginas de uma vez.
- Alterar filtros reinicia a paginação.
- Estado vazio, erro e carregamento ficam visualmente distintos.
- O detalhe trata `404` sem quebrar a navegação.

### B-FE-02 - Completar perfil do fisioterapeuta

**Responsável:** Joao V.

**Origem invertida:** atualização do fisioterapeuta de Dudu.

**Rota disponível:**

- `PUT /api/profissional/completar-perfil`

**Implementação Flutter:**

- Criar formulário de dados profissionais com validações equivalentes às exigidas pela API.
- Não solicitar `usuarioId` ou `profissionalId`; a API usa a identidade autenticada.
- Tratar `409` de CREFITO duplicado e erros de validação por campo.
- Atualizar o estado local somente após resposta de sucesso.

**Critérios de aceite:**

- Somente Profissional acessa a tela.
- CREFITO duplicado não sobrescreve os dados locais.
- Após salvar, a tela apresenta confirmação e mantém os valores enviados.

**Dependência conhecida:** ainda não existe `GET /api/profissional/me` com todos os campos privados para preencher o formulário. Até essa rota existir, não assumir que o detalhe público contém CPF e data de nascimento.

### B-FE-03 - Especialidades e vínculo do profissional

**Responsável:** Davi.

**Origem invertida:** especialidades de Caio.

**Rotas disponíveis:**

- `GET /api/especialidade`
- `GET /api/especialidade/{id}`
- `PUT /api/profissional/especialidade` - Profissional
- `POST /api/especialidade` - Clínica

**Implementação Flutter:**

- Criar seletor pesquisável de especialidades no perfil profissional.
- Persistir a seleção com `{ "especialidadeId": "..." }`.
- Criar tela simples de cadastro de especialidade exclusiva da Clínica.
- Atualizar o catálogo local imediatamente após `201`.
- Tratar `400`, `404` e `409` com mensagens específicas.

**Critérios de aceite:**

- Listagem de especialidades funciona mesmo antes do login.
- Profissional pode escolher ou trocar sua especialidade.
- Apenas Clínica vê a ação de criar especialidade.
- Especialidade duplicada não aparece duas vezes na lista.

### B-FE-04 - Grade e disponibilidade do fisioterapeuta

**Responsável:** Gabriel.

**Origem invertida:** disponibilidade e grade de horários de Liska.

**Status:** bloqueada por backend.

**Escopo do PDF ainda sem rotas:** configurar dias/turnos de trabalho e consultar horários livres calculados com os agendamentos existentes.

**Preparação permitida enquanto o backend não chega:**

- Criar modelos de apresentação e interfaces de repositório sem fixar URLs inexistentes.
- Criar calendário/grade com estados vazio, carregando e indisponível.
- Criar testes de widget usando repositório falso.

**Dependência para concluir:** contrato backend para consultar e alterar a grade do profissional e para consultar disponibilidade por profissional/período. Os nomes e payloads das rotas devem ser definidos pelo backend antes da integração.

### B-FE-05 - Atualização administrativa de fisioterapeuta

**Responsável:** Joao V.

**Status:** bloqueada por backend.

O PDF permite atualização pelo próprio profissional ou administrador, mas a rota atual `PUT /api/profissional/completar-perfil` aceita somente o papel Profissional. É necessária uma rota administrativa com ID explícito antes de criar esse fluxo no Flutter.

---

## Definition of Done para todas as tasks

- DTOs Flutter refletem o JSON real da API e tratam campos opcionais.
- Camada de UI não chama HTTP diretamente; usa o padrão de repository/service adotado pelo projeto.
- Estados de carregamento, vazio, sucesso, erro, `401`, `403`, `404` e `409` são tratados quando aplicáveis.
- Datas são enviadas em UTC/ISO 8601 e exibidas no fuso local.
- Telas respeitam o papel retornado por `/api/auth/me`.
- Há testes unitários de serialização/repositório e testes de widget para os fluxos principais.
- Nenhuma tela pede ao usuário o próprio `usuarioId` ou `perfilId`.
- A task inclui evidência de chamada contra a API real; mock isolado não encerra uma integração.

## Ordem sugerida

1. `FE-COMUM-01`.
2. Em paralelo: `A-FE-01`, `A-FE-02`, `B-FE-01`, `B-FE-03`.
3. Depois: `A-FE-03`, `B-FE-02` e `A-FE-04`.
4. Após novos contratos backend: `A-FE-05`, `A-FE-06`, `B-FE-04`, `B-FE-05`.
