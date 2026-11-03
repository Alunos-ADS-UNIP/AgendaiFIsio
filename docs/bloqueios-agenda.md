# Contrato de bloqueios da agenda

## Unidade e fuso horário

- Consultas e bloqueios de horário começam sempre em hora cheia (`minuto = 00`).
- Um bloqueio de horário dura exatamente 60 minutos e usa intervalo semiaberto `[início, fim)`.
- Datas e horas da API precisam conter offset explícito e são persistidas em UTC.
- Um bloqueio de dia representa o dia civil completo no fuso `America/Sao_Paulo`.

O aplicativo mobile já oferece somente botões de hora cheia no agendamento. Ele não envia nem
interpreta `status=Bloqueado`; bloqueios são recursos separados de consultas.

## Rotas do profissional autenticado

### Bloquear uma hora

`POST /api/agenda/bloqueios/horarios`

```json
{
  "dataHora": "2026-11-03T12:00:00Z",
  "motivo": "Compromisso pessoal"
}
```

### Bloquear um dia completo

`POST /api/agenda/bloqueios/dias`

```json
{
  "data": "2026-11-04",
  "motivo": "Congresso"
}
```

### Consultar

`GET /api/agenda/bloqueios?inicio=2026-11-01T00:00:00Z&fim=2026-12-01T00:00:00Z`

O período é obrigatório e limitado a 93 dias. Também existem `GET
/api/agenda/bloqueios/{id}` e `DELETE /api/agenda/bloqueios/{id}`. Um profissional nunca acessa
ou remove bloqueios de outro profissional.

## Conflitos e concorrência

O banco serializa alterações da mesma agenda com um advisory lock derivado do
`ProfissionalId`. Triggers impedem a coexistência entre consulta e bloqueio, mesmo quando a
gravação não passa pela API. Uma exclusion constraint impede intervalos de bloqueio
sobrepostos. Conflitos retornam HTTP 409.

## Migração e rollout

A migration converte agendamentos antigos com status `Bloqueado` para a tabela de intervalos e
remove somente o paciente técnico conhecido. O rollback recria os registros técnicos e expande
cada intervalo em agendamentos técnicos de uma hora, de `Inicio` até antes de `Fim`. O primeiro
horário preserva o ID do bloqueio quando ele estiver disponível; os demais recebem IDs próprios.
Se houver conflito de agenda, a transação falha em vez de perder bloqueios silenciosamente.

Ordem de publicação:

1. publicar o backend com as rotas novas;
2. integrar a tela de bloqueios do profissional no mobile;
3. manter agendamentos de pacientes em hora cheia, contrato já adotado pelo mobile;
4. não reintroduzir o fluxo de bloqueio por `POST /api/agendamento` ou pelo status `Bloqueado`.
