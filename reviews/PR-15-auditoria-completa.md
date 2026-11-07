# Auditoria completa — Backend PR #15

- PR: <https://github.com/Alunos-ADS-UNIP/AgendaiFIsio/pull/15>
- Autor: Gabriel-Barbosa10
- Branch da PR: `Branch-Disponibilidade`
- Commit auditado: `9fe38e58d2c4a0f68850b2f95113b5a260542db7`
- Branch local isolada: `review/pr-15-backend`
- Data da auditoria: 4 de novembro de 2026
- Decisão atual: **não fazer merge ainda**

## Resumo executivo

A PR está sem conflitos com a `main`, compila sem warnings, não possui dependências NuGet vulneráveis conhecidas e não quebrou a suíte oficial. As duas novas rotas têm autorização coerente e não foi encontrado vazamento de CPF, nascimento, motivo do bloqueio ou dados de outro paciente.

Por decisão de produto, a branch de revisão foi endurecida antes de qualquer merge: o MVP agora aceita somente os papéis `Paciente` e `Profissional`, a senha mínima passou para oito caracteres e os fluxos de Clínica/Administrador foram removidos. Um JWT legado assinado com o papel `Clinica` recebe `403` nas rotas autenticadas e não recupera o acesso administrativo antigo.

Mesmo assim, a PR ainda não deve ser integrada. A regra de cadastro completo é incompleta e pode ser contornada com dados semanticamente inválidos. Além disso, um profissional recém-criado nasce ativo, aparece na listagem e pode receber agendamentos sem preencher nome, CPF, CREFITO, telefone, nascimento ou especialidade. Isso contradiz a descrição do próprio commit da PR.

Também permanece a dependência de `NomeCompleto = "Cadastro Pendente"`, embora a decisão do produto seja mostrar essa condição na interface e não armazená-la como nome.

## O que a PR altera

1. Cria `GET /api/profissional/me`, exclusivo do papel `Profissional`, incluindo CPF e data de nascimento do próprio usuário.
2. Cria `GET /api/profissional/{id}/indisponibilidade`, acessível a usuários autenticados, agregando bloqueios e consultas não canceladas.
3. Impede o autoagendamento de paciente considerado incompleto e devolve HTTP `422`.
4. Adiciona `CadastroPacienteCompleto` para decidir se o paciente pode agendar.
5. Ajusta os dados da suíte antiga para incluir telefone, sem adicionar testes para as novas funcionalidades.

## Ambiente e testes executados

- macOS ARM64
- .NET SDK `10.0.301`
- PostgreSQL `16-alpine` em Docker, banco descartável exclusivo
- API em `Development`, `http://127.0.0.1:5187`
- JWT e banco configurados apenas para o ambiente de teste

### Resultado agregado

| Verificação | Resultado |
|---|---:|
| Build da API | 0 erros, 0 warnings |
| Build das migrations | 0 erros, 0 warnings |
| Build da regressão | 0 erros, 0 warnings |
| Migrations Up/Down e preservação de dados | 14/14 aprovadas |
| Suíte oficial de API, autorização e concorrência | 247/247 aprovadas |
| Auditoria complementar da PR #15 | 57/57 respostas confirmadas |
| Teste da integração JWT no Scalar | 1/1 aprovado |
| Dependências NuGet vulneráveis conhecidas | nenhuma encontrada |
| Operações expostas pelo OpenAPI | 29 em 27 caminhos |

As 57 verificações complementares confirmam o comportamento observado. Algumas delas demonstram defeitos — por exemplo, receber `201` ao agendar com profissional pendente não significa que a regra esteja correta.

## Cobertura das rotas

### Autenticação

| Rota | Cenários principais | Retorno observado |
|---|---|---|
| `POST /api/auth/register` | paciente/profissional válidos; papéis administrativos e desconhecidos | `200`; inválidos `400`; senha com 7 caracteres `400` |
| `POST /api/auth/login` | credencial válida e senha incorreta | `200`; incorreta `401` |
| `GET /api/auth/me` | token válido, ausente, inválido e expirado | `200`; inválidos `401` |
| `POST /api/auth/logout` | autenticado e anônimo | `204`; anônimo `401` |

Observação: após `POST /logout`, o mesmo token continuou funcionando em `/auth/me` com `200`. Isso corresponde ao logout stateless documentado, mas não revoga um token roubado.

### Pacientes

| Rota | Cenários principais | Retorno observado |
|---|---|---|
| `GET /api/paciente` | profissional, paciente, token legado Clínica e anônimo; paginação; busca por nome/CPF | profissional `200`; demais `401/403` |
| `GET /api/paciente/{id}` | profissional autorizado, outros papéis e inexistente | `200`, `401/403`, `404` |
| `GET /api/paciente/me` | próprio paciente e papel incorreto | `200`, `403` |
| `PUT /api/paciente/completar-perfil` | próprio paciente, papel incorreto e CPF inválido | `200`, `403`, `400` |
| `GET /api/paciente/{id}/historico-consultas` | profissional vinculado, não vinculado, outros papéis e inexistente | `200`, `404`, `401/403` |
| `GET /api/paciente/me/historico-consultas` | paginação, ordenação, limite e histórico vazio | `200` |

### Profissionais

| Rota | Cenários principais | Retorno observado |
|---|---|---|
| `GET /api/profissional` | autenticado/anônimo e filtros | `200`/`401` |
| `GET /api/profissional/{id}` | existente/inexistente e proteção dos campos privados | `200`/`404`; sem CPF/nascimento |
| `GET /api/profissional/me` | profissional, paciente e anônimo | `200`, `403`, `401` |
| `PUT /api/profissional/completar-perfil` | próprio profissional, papel incorreto, CPF/CREFITO | `200`, `403`, `400/409` |
| `PUT /api/profissional/especialidade` | válida, ausente e inexistente | `200`, `400`, `400` |
| `GET /api/profissional/{id}/indisponibilidade` | paciente/profissional, token legado Clínica, anônimo, período inválido, >93 dias, profissional inexistente/inativo | `200`, `403`, `401`, `400`, `404` |

Na indisponibilidade também foram conferidos: bloqueio de uma hora, consulta de uma hora, exclusão de consulta cancelada, fronteiras de intervalo e ausência de `id`, motivo e dados do paciente.

### Agendamentos

| Rota | Cenários principais | Retorno observado |
|---|---|---|
| `GET /api/agendamento` | agenda por paciente/profissional, token legado Clínica, filtros e isolamento | papéis do MVP `200`; legado `403`; anônimo `401` |
| `GET /api/agendamento/{id}` | visível, alheio e inexistente | `200`/`404` |
| `POST /api/agendamento` | papel, horário passado, hora quebrada, conflito, bloqueio, offset, cadastro pendente | `201`, `400`, `403`, `409`, `422` conforme o cenário |
| `POST /api/agendamento/administrativo` | rota removida do MVP | `405`; ausente do OpenAPI |
| `PUT /api/agendamento/{id}/reagendar` | dono, profissional vinculado, alheio, conflito, bloqueio e estados finais | `200`, `404`, `409`, `400` |
| `PATCH /api/agendamento/{id}/cancelar` | dono, alheio, inexistente e cancelamento repetido | `200`, `404`, `409` |

Foram repetidas oito rodadas de cenários concorrentes entre consultas, bloqueios e reagendamentos. As restrições do PostgreSQL impediram persistência simultânea incompatível.

### Bloqueios, especialidades e infraestrutura

| Rota | Cenários principais | Retorno observado |
|---|---|---|
| `GET /api/agenda/bloqueios` | próprio profissional, papéis incorretos, período ausente e >93 dias | `200`, `401/403`, `400` |
| `GET /api/agenda/bloqueios/{id}` | próprio e inexistente/alheio | `200`/`404` |
| `POST /api/agenda/bloqueios/horarios` | válido, hora quebrada, sobreposição e consulta existente | `201`, `400`, `409` |
| `POST /api/agenda/bloqueios/dias` | dia completo e conflito | `201`/`409` |
| `DELETE /api/agenda/bloqueios/{id}` | próprio e alheio | `204`/`404` |
| `GET /api/especialidade` e `/{id}` | acesso público, existente e inexistente | `200`/`404` |
| `POST /api/especialidade` | catálogo fechado do MVP | `405`; operação ausente do OpenAPI |
| `GET /health` | anônimo | `200` |
| `GET /openapi/v1.json` | Development | `200` |

## Achados bloqueadores

### P1 — profissional pendente continua ativo e recebe agendamento

**Evidência executada:**

1. cadastro de profissional sem completar perfil: `200`;
2. listagem `?ativo=true`: o profissional aparece com `NomeCompleto = "Cadastro Pendente"`;
3. consulta de indisponibilidade: `200`;
4. paciente completo agenda com esse profissional: `201`.

O Backend usa apenas `Ativo` para decidir se o profissional pode receber consultas. O cadastro inicial define `Ativo = true` pelo padrão da entidade. A nova regra não verifica o cadastro profissional.

**Mudança recomendada:** separar os conceitos de conta ativa e cadastro completo. Não usar `Ativo` como substituto de completude. Criar uma regra única `CadastroProfissionalCompleto`, definir quais campos são obrigatórios — inclusive se especialidade faz parte — e excluir profissionais incompletos da listagem e da criação de agendamento. Definir também quem pode alterar `Ativo`; sem administrador no MVP, não se deve tratá-lo implicitamente como aprovação de clínica.

### P1 — cadastro semanticamente inválido é considerado completo

**Evidência executada:** `PUT /api/paciente/completar-perfil` aceitou nascimento `2099-01-01`, telefone `x`, CEP `x`, estado `x` e demais campos de endereço `x`, retornando `200`. Em seguida, o paciente conseguiu agendar e recebeu `201`.

`CadastroPacienteCompleto` verifica principalmente se os textos não estão vazios. Ele não comprova que telefone, CEP, UF ou nascimento são válidos.

**Mudança recomendada:** validar no DTO e novamente na regra de domínio:

- nascimento entre uma data mínima acordada e hoje;
- telefone com 10 ou 11 dígitos;
- CEP com 8 dígitos;
- UF com 2 letras;
- limites de tamanho para nome e endereço;
- CPF usando a mesma validação de dígitos verificadores já existente.

### P1 — `Cadastro Pendente` ainda é dado de domínio

O cadastro grava literalmente `"Cadastro Pendente"` em `NomeCompleto` para paciente e profissional. A PR também adiciona uma constante com esse texto e o Mobile #9 procura exatamente a mesma frase. Isso contradiz a decisão de produto de mostrar uma mensagem na tela sem adulterar o nome.

**Mudança recomendada:**

1. novos perfis devem usar nome vazio ou um estado explícito de completude;
2. `/paciente/me` e `/profissional/me` devem expor `cadastroCompleto`;
3. Mobile deve usar o booleano, não comparar texto;
4. dados existentes com o placeholder devem ser corrigidos por migration de dados ou rotina controlada.

Essa alteração precisa ser coordenada com a PR Mobile #9; mudar somente o Backend quebra a detecção atual do primeiro acesso no front.

### P1 — funcionalidades novas não têm testes na suíte da PR

O único ajuste em `Tests/Regression/Program.cs` foi incluir telefone nos payloads antigos. Não há teste versionado para `/api/profissional/me`, `/indisponibilidade`, o `422`, profissional pendente ou qualidade dos dados do cadastro.

**Mudança recomendada:** incorporar os cenários relevantes de `Tests/pr15-route-audit.sh` à suíte oficial em C#, mantendo os testes de autorização e isolamento de dados no CI.

## Decisão de produto que ainda precisa ser confirmada

### P2 — qualquer profissional pode consultar a indisponibilidade de outro profissional?

Hoje paciente e profissional autenticados recebem `200`; o anônimo recebe `401` e um token legado
de Clínica recebe `403`. Não há exposição de dados privados, mas o contrato ainda deve declarar se
o acesso entre profissionais é proposital.

## Qualidade e dados

### P2 — `DataCadastro` do profissional é `0001-01-01`

O perfil recém-criado não inicializa `DataCadastro`. A rota nova `/api/profissional/me` repassa esse valor inválido. Definir `DateTime.UtcNow` na criação e corrigir registros existentes.

### P3 — detalhes menores

- A rota nova usa a string `"Profissional"` em vez de `PerfilDeUsuario.Profissional`.
- `IProfissionalService.cs` termina sem newline.
- O corpo da PR não descreve contrato, riscos ou como validar as novas rotas.
- A resposta agrega intervalos sem mesclá-los. Isso não falhou porque o banco impede sobreposição nos fluxos normais, mas vale documentar que o cliente pode receber vários intervalos consecutivos.

## Segurança e autenticação

### Pontos aprovados

- `/api/profissional/me` devolve dados privados somente ao próprio papel profissional.
- paciente e anônimo receberam `403` e `401`, respectivamente.
- o detalhe público do profissional não expõe CPF nem nascimento.
- indisponibilidade expõe somente `inicio` e `fim`.
- consultas canceladas deixam de aparecer como indisponibilidade.
- tokens ausentes, inválidos e expirados foram rejeitados.
- papéis administrativos não podem ser criados pelo cadastro público.
- o perfil Clínica/Administrador, seu seed e suas duas rotas de escrita foram removidos do MVP.
- um token legado com papel `Clinica`, mesmo corretamente assinado, recebeu `403` nas rotas do MVP.
- buscas usam LINQ/EF parametrizado; não foi encontrado SQL dinâmico nas rotas.
- BCrypt é usado para armazenar senhas.
- nenhuma dependência NuGet vulnerável foi encontrada nas fontes consultadas.

### Riscos anteriores à PR, recomendados para hardening separado

1. Não há rate limit ou bloqueio progressivo em login e cadastro. Seis tentativas inválidas consecutivas receberam `401`, nunca `429`.
2. Logout não revoga o JWT; o token permaneceu válido após receber `204`.
3. Existe uma chave JWT fixa versionada em `appsettings.Development.json`. O Render usa chave gerada em Production, mas ambientes de desenvolvimento compartilhados podem aceitar tokens forjados por quem conhece o repositório.
4. Alguns controladores devolvem `ex.Message` ao cliente, o que pode revelar detalhes internos em erros não previstos.

### Correção de segurança já aplicada na branch de revisão

A senha mínima passou de 6 para 8 caracteres em todos os DTOs de criação de conta. A suíte oficial agora testa que sete caracteres retornam `400`, o OpenAPI publica `minLength: 8` e todas as 247 regressões continuam aprovadas.

O seed `AdminSeed`, o papel `Clinica`, `POST /api/agendamento/administrativo` e `POST /api/especialidade` foram removidos. A leitura pública do catálogo de especialidades foi preservada, e alterações no catálogo agora exigem migration revisada.

Esses riscos merecem tasks próprias, mas não foram introduzidos pela PR #15. Não devem ser confundidos com os bloqueadores funcionais da integração de disponibilidade.

## Compatibilidade com a PR Mobile #9

O contrato principal é compatível:

- Mobile chama `GET /api/profissional/{id}/indisponibilidade` com datas UTC;
- Mobile divide 366 dias em janelas de 90 dias;
- Backend aceita no máximo 93 dias;
- resposta `inicio`/`fim` corresponde ao parser do Mobile;
- Mobile usa `GET /api/profissional/me` para CPF e nascimento.

O problema é o placeholder: Backend e Mobile estão acoplados a `Cadastro Pendente`. A correção precisa ser feita nos dois repositórios antes do merge conjunto.

## Critério objetivo para liberar o merge

1. remover o placeholder do nome e ajustar o Mobile para um estado explícito de completude;
2. impedir profissional incompleto de aparecer como agendável e receber consulta;
3. fortalecer as validações dos campos usados para declarar o paciente completo;
4. confirmar se profissionais podem consultar a indisponibilidade de outros profissionais;
5. adicionar testes das rotas novas e das regras acima ao CI;
6. repetir migrations, 247 regressões e a auditoria complementar sem falhas;
7. só então atualizar a branch com a `main`, revisar o diff final e fazer o merge.
