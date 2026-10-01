# Code review — PR #3 / branch-caio

PR: https://github.com/Alunos-ADS-UNIP/AgendaiFIsio/pull/3

Commit revisado: `68e714f30af72a1bbe37f7b8a288d05f8e59249e`, comparado com `origin/main` (`09c1e6c`). Revisão de código, compilação e testes HTTP/SQL Server em banco temporário exclusivo. Nenhum comentário foi publicado no GitHub e o código da PR não foi corrigido nesta revisão.

**Parecer: solicitar alterações.** O catálogo funciona, mas o vínculo com profissionais não está garantido. Há também uma dependência de autorização insegura e risco de perda de dados nas migrações.

## Aderência ao pedido

| Critério | Resultado |
|---|---|
| Listar todas as especialidades cadastradas | Atendido para usuários autenticados: GET retorna Id e Nome, ordenados por nome, sem paginação |
| Impedir repetição de nome | Parcial: índice único e verificação prévia funcionam; equivalência por acento depende da collation, sem regra explícita |
| Disponibilidade imediata após criar | Atendido no catálogo: POST salva e o GET seguinte já retorna a especialidade |
| Vincular a especialidade ao profissional | Não garantido: perfil continua como texto livre e a segunda migração remove a FK |
| Usar catálogo nos filtros | Possível enviar o nome, mas o filtro continua textual por substring; não usa o Id retornado pelo catálogo |

## Achados

### 1. [P1] Catálogo desconectado do perfil do profissional

Local: `Migrations/20260924120000_RevertePerfilEspecialidadeParaTexto.cs:33-43`, `Context/AgendaiFisioDbContext.cs:50-55`.

A primeira migração cria EspecialidadeId e a FK, mas a segunda os exclui. O serviço de profissional continua aceitando e gravando qualquer texto, sem consultar Especialidades. Reproduzido: PUT de perfil com `especialidade="NaoCatalogada"` retornou 200; o nome não existia nem passou a existir no catálogo.

Isso permite perfis fora das opções disponíveis nos filtros e não estabelece a vinculação pedida. Criar um item e vê-lo no GET não comprova a integração com o profissional. A correção preferível é preservar o identificador/FK, receber e validar o identificador no perfil e filtrar por ele. Se a decisão do produto for manter texto, é necessário ao menos validar sua existência e manter uma representação canônica consistente; hoje nenhuma dessas garantias existe.

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

O requisito não define literalmente a equivalência por acento; entretanto, os comentários afirmam que ela existe. É necessário decidir e documentar essa regra e aplicá-la na consulta e no índice. Não presumir que todo SQL Server ignora acentos ou maiúsculas.

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

## Observações e decisões de produto

- A criação foi restringida a Clinica, mas o requisito informado não define quem pode criar. Confirmar se somente a clínica pode cadastrar ou se profissionais também devem poder fazê-lo. Não considerei a restrição, isoladamente, um bug.
- O GET exige login. Isso funciona no fluxo de completar perfil após autenticação. Se a lista precisar aparecer antes do registro/login, será necessário ajustar a política; o pedido não determina esse detalhe.
- O requisito não determina uma ou várias especialidades por profissional. Não foi presumida uma relação muitos-para-muitos.
- O filtro atual usa Contains: selecionar um nome pode incluir outros nomes que contenham o mesmo trecho. A busca exata pelo identificador elimina essa ambiguidade se o objetivo for selecionar uma categoria do catálogo.
- A PR não acrescenta testes automatizados. Recomenda-se cobrir os cenários reproduzidos, concorrência de nomes iguais e migração com dados legados.
- Há alterações de bin/obj na PR. Esses artefatos já estavam versionados na base; devem ser removidos do versionamento em uma limpeza apropriada.
- A compilação da branch passou com 0 erros e 35 ocorrências de avisos, incluindo NU1903 repetido na restauração/compilação e os avisos de nulidade preexistentes. As correções locais anteriores não estão incorporadas à PR; não são novos defeitos de especialidades.

## Evidências de execução

```text
MIGRATION original_length=109 final_length=100 data_preserved=False
COLLATION SQL_Latin1_General_CP1_CI_AS
SELF_ADMIN register=200 login=200 create=201
LIST status=200 newly_created_visible=True
DUPLICATE_CASE_SPACE status=409
ACCENTS first=201 second=201
SHORT_AFTER_TRIM status=201 name=a
CREATED_LOCATION get_status=404
UNCATALOGUED_PROFILE update_status=200 appears_in_catalog=False
NON_DUPLICATE_DB_FAILURE status=409 body={"erro":"Já existe uma especialidade cadastrada com este nome."}
Temporary review database removed.
```

Também foi verificada a consistência do modelo com o snapshot: não há mudanças pendentes. Isso não elimina os problemas de negócio e de transformação de dados descritos acima.

O banco temporário foi removido e o processo de teste encerrado. O banco de desenvolvimento do usuário não recebeu as migrações desta branch.
