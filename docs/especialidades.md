# Especialidades — regras da feature

**Status:** implementado no backend (Tasks 1–4 e a parte de backend da Task 6 do documento de
revisão da PR #3). **Responsável pelo registro:** assistente (Claude) a pedido de Caio Teixeira.
**Data:** 2026-10-01.

Este arquivo registra as decisões de produto já fechadas e a decisão técnica que ainda depende de
confirmação do solicitante, conforme pedido pela Task 1 do documento de revisão. Nenhuma aprovação
é inventada aqui: o que não foi confirmado por quem pediu a feature está marcado como tal.

## Comportamento

- O profissional escolhe **uma** especialidade do catálogo, no cadastro (opcional) ou depois,
  editando o perfil. Não existe seleção múltipla.
- A interface busca por nome só para **localizar** uma opção já existente. Texto digitado nunca
  se torna uma especialidade nem é salvo como tal — só o `EspecialidadeId` da opção escolhida é
  persistido.
- Criar uma conta sem escolher especialidade é permitido. O vínculo pode ser preenchido depois,
  a qualquer momento, por `PUT /api/profissional/especialidade`.
- O catálogo do MVP é fechado e carregado pela migration. Não existe rota para criar, editar ou
  excluir especialidades durante a execução da aplicação.
- A listagem (`GET /api/especialidade`, `GET /api/especialidade/{id}`) é pública (sem login),
  porque a tela de cadastro do profissional precisa dela antes de existir qualquer token. Nenhuma
  outra rota do controller é anônima.

## Catálogo inicial aprovado (16 itens)

Grafia exata, preservando acentos e prefixos — carregada pela migration
`20261001120000_CriaCatalogoEspecialidadesEVinculo` com IDs fixos (ver
`Entities/EspecialidadeCatalogoSeed.cs`):

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

A API e qualquer seletor devem exibi-los em ordem alfabética (é o que `GET /api/especialidade`
já devolve). Essa é a lista do produto, não uma validação de títulos ou registro profissional —
e não existe mapeamento automático entre nomes antigos de perfis e esses 16 itens (ver seção de
migração abaixo).

## Comparação e busca de nomes

A API preserva a grafia aprovada do catálogo. Como o catálogo é fechado no MVP, a busca textual é
responsabilidade do frontend sobre a lista devolvida por `GET /api/especialidade`; ela não cria
novos registros. A migration continua usando comparação sem diferença de caixa ou acento apenas
para tentar vincular dados legados aos itens aprovados.

## Seleção única e vínculo (`EspecialidadeId`)

- `Profissional.EspecialidadeId` é um `Guid?` com FK para `Especialidade`, sem tabela associativa.
- `PUT /api/profissional/completar-perfil` (perfil Profissional) **não aceita mais** o campo de
  texto `especialidade` — ele foi removido do contrato de escrita. Omitir o assunto nesse PUT
  nunca altera a especialidade.
- `PUT /api/profissional/especialidade` é a única rota que define ou troca o vínculo. O corpo
  exige `especialidadeId` (`Guid`); `null` é rejeitado (400) e o id precisa existir no catálogo
  (senão, 400). Reenviar o mesmo id mantém o vínculo; outro id válido substitui a FK.
- Consultas (`GET /api/profissional`, `GET /api/profissional/{id}`) devolvem
  `especialidade: { id, nome }` quando há vínculo e `especialidade: null` quando não há.
- O filtro de listagem usa `especialidadeId` (igualdade exata). Sem esse parâmetro, profissionais
  sem especialidade também aparecem.

## Migração dos dados antigos

A coluna de texto livre (`Profissionais.Especialidade`, mapeada hoje para a propriedade
`Profissional.EspecialidadeTextoLegado`) **foi preservada**, não apagada. A migration
`20261001120000_CriaCatalogoEspecialidadesEVinculo` liga `EspecialidadeId` a um item do catálogo
somente quando o texto bate, aparado e sem diferença de caixa/acento, com um dos 16 nomes
aprovados. Ela **não** cria especialidades novas a partir de texto não reconhecido e **não** faz
equivalência semântica automática — por exemplo, um perfil antigo com `"Ortopedia"` não é ligado a
`Traumato-Ortopédica`, e `"Respiratória"` não é ligado a `Fisioterapia Respiratória`. Esses casos
exigem decisão humana revisada, não aproximação automática.

Para listar os profissionais cujo texto antigo não encontrou correspondência (pendentes de revisão
manual), depois de aplicar a migration:

```sql
SELECT Id, Especialidade
FROM Profissionais
WHERE EspecialidadeId IS NULL AND LTRIM(RTRIM(Especialidade)) <> '';
```

A coluna antiga só deve ser removida numa migration futura, depois que alguém revisar essas
divergências — não antes.

## Papéis do MVP

O autocadastro público (`POST /api/auth/register`) e as rotas autenticadas aceitam somente
`Paciente` e `Profissional`. Não existe perfil administrativo ou de clínica no MVP.

## Fora do escopo desta entrega (backend)

- **Task 5 (seletor com busca no frontend):** este repositório contém só a API; não há tela de
  cadastro/edição aqui. Fica pendente até alguém apontar o repositório/responsável do frontend.
- **Alteração do catálogo:** não há POST, PUT, PATCH ou DELETE de especialidade no MVP. Mudanças na
  lista exigem migration revisada.
