# Especialidades — correções da PR e tasks atualizadas

Caio, este documento reúne as correções da PR e as regras atualizadas para entregar a feature. Os critérios abaixo substituem as alternativas anteriores sobre lista inicial, múltipla seleção e seleção obrigatória no cadastro.

**PR revisada:** [#3 — branch-caio](https://github.com/Alunos-ADS-UNIP/AgendaiFIsio/pull/3). **Commit revisado:** `68e714f30af72a1bbe37f7b8a288d05f8e59249e`. As linhas e evidências da revisão se referem a esse commit. Esta atualização é uma especificação de trabalho, não a confirmação de que o código já foi corrigido.

## Comportamento definido para a feature

O profissional escolhe uma especialidade existente no catálogo, durante o cadastro ou posteriormente ao editar/completar o perfil. A interface oferece seleção única com busca pelo nome. Digitar serve exclusivamente para localizar uma opção; não cadastra especialidades e não é salvo como especialidade do profissional.

- **Catálogo:** usar o rótulo “Especialidade” e os nomes da lista inicial abaixo, fornecidos pelo solicitante. A lista é do produto; não implica validação de títulos ou registro profissional.
- **Uma seleção por profissional:** usar `EspecialidadeId`, sem lista de IDs ou tabela associativa nesta entrega.
- **Preenchimento posterior permitido:** a conta pode ser criada sem especialidade. O campo é opcional no cadastro e o vínculo pode ser preenchido depois; quando houver seleção, seu ID deve existir no catálogo. Não introduzir bloqueio de ativação ou outra exigência não solicitada.
- **Busca e escolha:** o profissional pode buscar por nome, selecionar uma opção e substituí-la posteriormente. Não há ação “criar nova especialidade” no seu formulário.
- **Administração:** somente administradores confiáveis podem acrescentar especialidades. O profissional comum não cria itens nem pela interface nem por chamada direta à API.
- **Acesso:** disponibilizar a listagem necessária antes do login para alimentar o cadastro. Após a autenticação, o profissional altera somente o próprio perfil.
- **Persistência:** salvar o ID selecionado, nunca o texto da busca. Consultas retornam ID e nome; filtros de profissionais utilizam o ID.
- **Carga inicial:** inserir os itens aprovados sem duplicar registros nem trocar IDs em reexecuções. A quantidade inicial é 16 por corresponder à lista fornecida; não é limite de capacidade do catálogo.

**Proposta técnica ainda a confirmar — comparação dos nomes:** ignorar maiúsculas/minúsculas e acentos, remover espaços nas extremidades e reduzir espaços internos consecutivos a um. Usar a mesma regra na carga, na unicidade administrativa e na busca, mantendo a grafia de exibição aprovada. Exemplo: buscar `aquatica` deve localizar `Aquática`; cadastrar administrativamente ` aquatica ` deve conflitar com esse item. A decisão sobre essa comparação não foi explicitada pelo solicitante e não deve ser registrada como já aprovada. Confirmá-la na Task 1 antes de fixar a implementação da unicidade. As demais regras e a lista abaixo já orientam as tasks.

## Lista inicial aprovada

Preservar exatamente estes nomes de exibição, inclusive acentos e prefixos:

1. Acupuntura
2. Aquática
3. Cardiovascular
4. Dermatofuncional
5. Fisioterapia do Trabalho
6. Esportiva
7. Gerontologia
8. Neurofuncional
9. Oncologia
10. Osteopatia
11. Quiropraxia
12. Reumatologia
13. Fisioterapia Respiratória
14. Fisioterapia em Saúde da Mulher
15. Traumato-Ortopédica
16. Terapia Intensiva

A ordem acima reproduz a lista aprovada; a API e o seletor devem apresentá-la em ordem alfabética. Não adicionar automaticamente nomes mencionados como exemplos antigos ou presentes em perfis legados. Em particular, não substituir `Traumato-Ortopédica` por `Ortopedia` nem `Fisioterapia Respiratória` por `Respiratória`. Eventuais equivalências de dados antigos exigem mapeamento explícito revisado, não aproximação automática.

## Problemas por prioridade

### 1. [P1] Catálogo desconectado do perfil do profissional

Local: `Migrations/20260924120000_RevertePerfilEspecialidadeParaTexto.cs:33-43`, `Context/AgendaiFisioDbContext.cs:50-55`.

A primeira migração cria EspecialidadeId e a FK, mas a segunda os exclui. O serviço de profissional continua aceitando e gravando qualquer texto, sem consultar Especialidades. Reproduzido: PUT de perfil com `especialidade="NaoCatalogada"` retornou 200; o nome não existia nem passou a existir no catálogo.

Isso permite perfis fora das opções disponíveis nos filtros e não estabelece a vinculação pedida. Criar um item e vê-lo no GET não comprova a integração com o profissional. Nesta entrega, substituir texto livre pela FK opcional `EspecialidadeId`, permitindo uma especialidade por profissional; validar o ID quando informado e filtrar por ele. Manter apenas uma validação de texto não atende às Tasks 3 e 4.

### 2. [P1] A nova permissão administrativa pode ser obtida no cadastro público

Local novo: `Controllers/EspecialidadeController.cs:40-42`. Dependência preexistente: `Services/Auth/AuthService.cs:43` e `:138`; `Constants/PerfilDeUsuario.cs` define Admin como `Clinica`.

Reprodução completa na branch: POST register com `tipoUsuario="Clinica"` → 200; login → 200 com token; POST especialidade com esse token → 201. Não foi necessário acesso administrativo prévio nem falsificar token.

A origem é o cadastro de papéis arbitrários já presente na base, mas esta PR passa a depender dele para proteger uma nova operação. Corrigir a atribuição de papéis antes de disponibilizar o endpoint. As correções locais feitas na conversa anterior ainda não fazem parte deste commit. Com essas correções, será necessário provisionar contas Clinica por um fluxo confiável, se esse for o papel escolhido.

### 3. [P1] Migrações eliminam dados antigos sem aviso

Local: `Migrations/20260921120000_CriaEspecialidades.cs:43-58`.

O SQL usa LEFT(..., 100) e depois elimina a coluna original. A segunda migração reconstrói o texto a partir do valor já reduzido. Reproduzido com banco na migração anterior: especialidade de 109 caracteres terminou com 100 após aplicar as duas migrações. Dois textos diferentes com os mesmos primeiros 100 caracteres também são agrupados.

O risco depende de existirem dados legados maiores que 100 caracteres; a coluna anterior era nvarchar(max). O limite no DTO atual não garante a qualidade de registros antigos ou importados. Deve haver verificação prévia e uma estratégia explícita para esses dados, preservando-os ou impedindo a migração até sua correção. Não truncar silenciosamente.

### 4. [P2] A regra de duplicidade por acento anunciada no código não é implementada

Local: `Context/AgendaiFisioDbContext.cs:50-55`, `Services/Especialidade/EspecialidadeService.cs:35-36`.

Não há collation explícita nem chave normalizada. No SQL Server de teste, a collation foi SQL_Latin1_General_CP1_CI_AS: ignora caixa e distingue acentos. `Respiratória` e `Respiratoria` retornaram 201 cada. Já `Ortopedia` e ` ortopedia ` retornaram 201 e 409.

Na revisão original, a equivalência por acento não estava definida, embora os comentários a afirmassem. A proposta técnica de comparação abaixo deve ser confirmada e aplicada à consulta e ao índice; não presumir que todo SQL Server ignora acentos ou maiúsculas.

### 5. [P2] Qualquer falha de gravação é apresentada como nome duplicado

Local: `Services/Especialidade/EspecialidadeService.cs:50-53`.

O catch converte todo DbUpdateException em duplicidade. Reproduzido com uma falha SQL controlada de INSERT no banco temporário, sem nome repetido: a API devolveu 409 e “Já existe uma especialidade cadastrada com este nome.”

Isso orienta o cliente a corrigir o nome quando a causa é interna. Restringir o tratamento aos erros de chave única relevantes (SQL Server 2601/2627) e deixar os demais seguirem o tratamento de erro interno, com registro de diagnóstico.

### 6. [P2] POST devolve Location para uma rota inexistente

Local: `Controllers/EspecialidadeController.cs:51-52`.

O retorno 201 aponta para `api/especialidade/{id}`, mas o controller só possui GET da coleção e POST. Mesmo acessando a forma absoluta esperada `/api/especialidade/{id}` com token válido, o resultado foi 404. Além disso, o endereço retornado é relativo, sem barra inicial.

Implementar GET por identificador e usar CreatedAtAction, ou ajustar a resposta para não anunciar um recurso que não pode ser recuperado.

### 7. [P2] Validação de tamanho ocorre antes de remover espaços

Local: `Services/Especialidade/EspecialidadeService.cs:33`, `DTOs/Especialidade/EspecialidadeCreateDTO.cs:8-10`.

O DTO aceita três caracteres, mas o serviço altera o valor depois da validação. Reproduzido: `{ "nome": " a " }` retornou 201 e persistiu `a`, descumprindo o mínimo de três caracteres definido pelo próprio DTO. Validar o nome já normalizado antes de consultar e salvar.

## Tasks para concluir a feature

### Task 1 — Documentar as regras e finalizar a comparação dos nomes

**Descrição:** consolidar as definições acima na documentação da feature. Não reabrir a lista inicial ou implementar seleção múltipla.

**Critérios de aceite:**

- [ ] Registrar o catálogo como opções de especialidade do profissional, com os 16 nomes fornecidos e sua grafia exata.
- [ ] Registrar seleção única, opcional na criação da conta e disponível posteriormente no próprio perfil.
- [ ] Registrar que buscar pelo nome apenas filtra as opções e que texto digitado não se torna especialidade.
- [ ] Registrar criação exclusivamente administrativa, identificando o papel técnico — atualmente `Clinica` — e seu provisionamento confiável.
- [ ] Registrar listagem disponível no cadastro antes da autenticação e edição do perfil protegida após o login.
- [ ] Confirmar a proposta de comparação de caixa, acentos e espaços; registrar exemplos esperados e a decisão final antes de implementar a unicidade.
- [ ] Versionar as regras, a lista e a confirmação da comparação, identificando o responsável e a data sem inventar aprovações.

**Dependência:** nenhuma. Lista e fluxo estão definidos; a confirmação restante é a comparação dos nomes.

### Task 2 — Carregar o catálogo inicial aprovado

**Descrição:** implementar a carga dos 16 itens usando a entidade `Especialidade` e sua configuração existentes.

**Critérios de aceite:**

- [ ] Inserir todos os nomes da lista aprovada, sem nomes extras inventados ou gerados a partir de perfis antigos.
- [ ] Documentar o comando/mecanismo de inicialização por ambiente e sua ordem em relação às migrações. O catálogo fica disponível após essa execução.
- [ ] Reexecutar a carga sem duplicar itens, trocar IDs, apagar itens administrativos ou recriar registros existentes.
- [ ] Reutilizar o ID de um item já existente equivalente pela regra confirmada. Revisar divergências de grafia e colisões antes de atualizar dados existentes; não substituir IDs para corrigir o nome exibido.
- [ ] Manter índice único de nome, com comparação compatível com a regra da Task 1, sem depender da collation padrão do ambiente.
- [ ] Aplicar a mesma regra de comparação na carga, no POST administrativo e na proteção de unicidade do banco.
- [ ] Listar ID e nome de todos os itens em ordem alfabética; não fixar o seletor em 16 posições.
- [ ] Tornar itens criados por administrador disponíveis na próxima listagem e seleção, sem reiniciar o sistema.
- [ ] Testar banco vazio, banco parcialmente preenchido e execução repetida. Em banco vazio, a carga inicial resulta nos 16 nomes exatos; em banco existente, preservar os demais itens legítimos.

**Dependência:** Task 1, incluindo a confirmação da comparação.

### Task 3 — Criar o vínculo único e migrar dados antigos com segurança

**Descrição:** substituir `Profissional.Especialidade` como texto livre por `EspecialidadeId` opcional, relacionado ao catálogo.

**Critérios de aceite:**

- [ ] Usar `Guid? EspecialidadeId`, navegação opcional e FK para `Especialidade`; não criar tabela associativa.
- [ ] Permitir `null` para o profissional que ainda não escolheu. Não preencher uma opção por padrão nem criar o item “Não informado”.
- [ ] Criar os índices e a FK necessários e manter modelo, snapshot e banco consistentes.
- [ ] Associar textos legados ao catálogo pela regra aprovada antes de remover a coluna antiga. Valores vazios de perfis incompletos podem virar `null`.
- [ ] Relatar valores não vazios sem correspondência ou ambíguos, com ID do profissional e texto original, para revisão. Não criar itens do catálogo automaticamente.
- [ ] Tratar diferenças semânticas por mapeamento explicitamente revisado: `Ortopedia` não é automaticamente `Traumato-Ortopédica`, nem `Respiratória` é automaticamente `Fisioterapia Respiratória`.
- [ ] Não truncar textos, descartar registros ou converter divergências em `null` para esconder pendências. Incluir valores acima de 100 caracteres na revisão.
- [ ] Preservar a coluna original e impedir sua remoção enquanto houver divergências não resolvidas. Só concluir a remoção após conferir os vínculos e a preservação dos dados.
- [ ] Restringir a exclusão de uma especialidade vinculada, sem apagar profissionais ou deixar referências inválidas. Não é necessário implementar DELETE para atender à integridade.
- [ ] Testar a transição em banco novo e com dados legados, incluindo perfis sem seleção.

**Cuidados com as migrações da PR:** verificar onde as duas migrações já foram aplicadas. Não executar a cadeia que trunca e remove textos em um banco com dados esperando que uma migração posterior os recupere. Definir uma transição segura antes de aplicar; não reescrever histórico aplicado em ambientes compartilhados. Dados já perdidos exigem análise de recuperação a partir de uma fonte preservada.

**Dependências:** Tasks 1 e 2.

### Task 4 — Receber e consultar uma especialidade por ID

**Descrição:** ajustar cadastro, edição, consultas e filtros para a seleção única opcional.

**Critérios de aceite:**

- [ ] O cadastro de profissional aceita um único `EspecialidadeId` opcional. Omitir o campo ou enviar `null` permite cadastrar a conta sem seleção; quando informado, o ID é validado e persistido junto do perfil.
- [ ] Não adicionar esse campo apenas a um DTO sem uso: integrar ao fluxo efetivamente chamado pela tela de cadastro de profissional.
- [ ] A edição autenticada permite preencher a especialidade posteriormente ou substituir a anterior. Omitir o campo preserva o vínculo atual; documentar e testar essa semântica, sem confundir ausência do campo com remoção.
- [ ] Não implementar remoção implícita ao limpar a busca na interface. Uma operação de seleção exige um ID válido; um pedido de edição com `EspecialidadeId: null` é rejeitado com mensagem clara, preservando a seleção existente. O cadastro inicial sem escolha continua permitido.
- [ ] Verificar a existência do ID antes de salvar. GUID malformado, `Guid.Empty`, ID inexistente, texto livre ou uma coleção no lugar do ID único retornam 400 com mensagem clara.
- [ ] Validar a especialidade antes de persistir conta/perfil ou usar transação: um ID inválido não deixa conta criada parcialmente nem modifica outros campos do perfil.
- [ ] Rejeitar o antigo campo de texto `Especialidade` nos contratos de escrita, sem transformá-lo em item novo ou aceitá-lo como seleção. Documentar a incompatibilidade com o contrato antigo.
- [ ] Retornar `especialidade: { id, nome }` quando houver vínculo e `especialidade: null` quando ainda não houver seleção.
- [ ] Filtrar profissionais por um único parâmetro `especialidadeId`, usando igualdade sobre a FK. Sem o filtro, incluir também profissionais sem seleção, respeitados os demais filtros.
- [ ] Reenviar o mesmo ID mantém um único vínculo; trocar o ID substitui a FK atomicamente.
- [ ] Documentar exemplos de cadastro sem seleção, cadastro com seleção, preenchimento posterior, troca e consulta/filtro. Não usar `EspecialidadeIds`.

**Dependência:** Task 3.

### Task 5 — Implementar seletor único com busca pelo nome

**Descrição:** oferecer ao profissional uma lista pesquisável de especialidades, no cadastro e na edição posterior do perfil.

**Critérios de aceite:**

- [ ] Carregar opções da API; não duplicar o catálogo em uma lista fixa no frontend.
- [ ] Exibir o campo “Especialidade” com seleção única e busca pelo nome. Não usar múltipla seleção.
- [ ] Digitar um trecho apenas filtra os nomes existentes conforme a comparação definida; não mostrar botão/opção para criar item.
- [ ] Separar texto de busca e opção selecionada. Digitar sem escolher não produz `EspecialidadeId` válido; limpar a busca não apaga o vínculo.
- [ ] No cadastro, permitir continuar sem escolher, enviando o campo ausente ou `null`. Se o usuário quiser informar sua especialidade, deverá selecionar uma opção real, não apenas digitar.
- [ ] Na edição, carregar a opção já vinculada; para perfil sem vínculo, mostrar “Não selecionada” como estado visual, sem criar um item no banco.
- [ ] Enviar somente o ID da opção escolhida em `EspecialidadeId` e preservar outros campos do formulário.
- [ ] Permitir trocar a seleção por outra opção existente, confirmando o salvamento no backend.
- [ ] Mostrar carregamento, erro, catálogo vazio e busca sem resultados, com ação para tentar carregar novamente.
- [ ] Se a carga falhar, manter possível o cadastro sem especialidade, mas não inventar uma seleção. Na edição, preservar o vínculo existente e os dados já preenchidos.
- [ ] Testar o payload enviado: contém no máximo um ID; o texto usado na busca não é enviado como especialidade.

**Dependências:** contratos das Tasks 1 e 4.

**Interface:** o repositório revisado contém a API e não apresentou uma tela de cadastro. Identificar o repositório/tela e o responsável pelo frontend e vincular essa entrega à feature. Alterar apenas DTOs ou demonstrar rotas no Scalar não conclui esta task.

### Task 6 — Garantir acesso, erros corretos e testes do fluxo

**Descrição:** corrigir os achados restantes da PR e validar o fluxo com seleção única opcional.

**Critérios de aceite:**

- [ ] Permitir acesso anônimo ao GET de listagem necessário ao cadastro, expondo apenas ID e nome. Não liberar o controller inteiro nem as operações de escrita.
- [ ] Manter POST administrativo: usuário comum recebe 403; chamada sem autenticação recebe 401.
- [ ] Impedir que o cadastro público conceda `Clinica` ou outro papel administrativo. Provisionar administradores por mecanismo confiável e documentado.
- [ ] Autorizar a edição do profissional pelo usuário autenticado proprietário do perfil; um usuário não altera a seleção de outro.
- [ ] Retornar 409 para nomes administrativos duplicados, inclusive requisições concorrentes, segundo a comparação confirmada e o índice único.
- [ ] Distinguir violação de unicidade de outras falhas SQL. Não tratar toda `DbUpdateException`/`InvalidOperationException` como duplicidade.
- [ ] Registrar erros inesperados em log e retornar 500 genérico, sem `ex.Message`, SQL, credenciais ou stack trace na resposta.
- [ ] Normalizar antes de validar o comprimento do nome no cadastro administrativo. `" a "` deve retornar 400 enquanto o mínimo for três caracteres.
- [ ] Corrigir o 201 de criação: se houver `Location`, a URL deve resolver para uma rota existente que retorne o item criado, respeitando sua política de acesso; preferir GET por ID com `CreatedAtAction`.
- [ ] Testar cadastro sem seleção e com seleção válida, preenchimento posterior, consulta, troca de especialidade e filtro por ID.
- [ ] Testar ID malformado, vazio e inexistente, envio de texto livre e tentativa de enviar múltiplos IDs; nenhum deles pode produzir gravação parcial.
- [ ] Testar edição sem o campo preservando a FK, edição com `null` sendo rejeitada, repetição do mesmo ID e troca por outro ID.
- [ ] Verificar que os 16 itens iniciais estão disponíveis e que uma segunda carga preserva seus IDs.
- [ ] Testar a interface com busca sem seleção, erro de carga, nova tentativa e opção previamente selecionada.
- [ ] Testar migração com textos correspondentes, vazios e sem correspondência, garantindo revisão e preservação dos últimos.
- [ ] Automatizar as regressões e registrar comandos/resultados na PR. Usar banco isolado para testes de migração e falhas controladas.

**Dependências:** Tasks 2 a 5. Segurança e tratamento de erros podem ser corrigidos antes; o aceite completo exige a integração.

## Rastreabilidade das correções da PR

| Achado | Tasks | Evidência de aceite |
| --- | --- | --- |
| 1. Texto livre sem vínculo | 3, 4 e 5 | FK opcional validada, seletor único e filtro por ID |
| 2. Autoadministração no cadastro | 1 e 6 | Cadastro não concede privilégio; profissional comum não cria itens |
| 3. Truncamento de dados | 3 | Dados preservados e divergências revisadas antes de remover a coluna |
| 4. Comparação implícita de nomes | 1, 2 e 6 | Regra confirmada e consistente na carga, API e banco |
| 5. Erro interno apresentado como duplicidade | 6 | 409 só para duplicidade; demais falhas internas retornam 500 genérico e geram log |
| 6. Location inexistente | 6 | URL fornecida recupera o item criado |
| 7. Comprimento validado antes do Trim | 6 | Nome normalizado inválido rejeitado antes de salvar |

## Checklist de entrega

- [ ] Documentação e lista inicial atualizadas; confirmar somente a decisão de comparação ainda pendente, sem reabrir seleção única ou inventar obrigatoriedade no cadastro.
- [ ] Catálogo inicial carregado e reexecutável, sem limite fixo de 16 opções na aplicação.
- [ ] Migração segura e FK opcional, sem tabela associativa, texto livre ou seleção automática.
- [ ] Cadastro com ou sem escolha e preenchimento posterior funcionando com o mesmo contrato de ID.
- [ ] Interface de seleção única integrada, com busca que apenas localiza itens existentes.
- [ ] Proteção administrativa efetiva, erros corretos e regressões dos sete achados verificados.
- [ ] Build e testes executados, identificando dependências e problemas preexistentes.
- [ ] Alterações geradas em `bin/` e `obj/` retiradas do escopo de código; coordenar sua exclusão do versionamento e `.gitignore` sem descartar trabalho de outros colaboradores.
- [ ] Título e descrição da PR atualizados com comportamento final, instruções de inicialização/migração e evidências de teste. Vincular a entrega do frontend quando estiver em outro repositório.

**Ordem de trabalho:** concluir o registro da Task 1 e confirmar a comparação → carga inicial (2) → vínculo e migração (3) → contratos e filtros (4) → seletor (5) → validação integrada (6). Revisar as migrações antes de aplicá-las a dados existentes.

## Observações sobre a branch revisada

- As evidências dos sete achados são históricas do commit revisado; não foram repetidas nesta atualização documental.
- Os exemplos antigos `Ortopedia` e `Respiratória` usados nos testes não alteram os nomes da lista inicial aprovada.
- A branch revisada não continha testes da feature. A compilação passou com 0 erros e 35 ocorrências de avisos, incluindo alertas preexistentes de nulidade e NU1903 repetido na restauração/compilação.
- As correções locais anteriores de autenticação, CPF, privacidade e dependências não estavam no commit revisado. Coordenar sua integração e não presumir que já estão na PR.
