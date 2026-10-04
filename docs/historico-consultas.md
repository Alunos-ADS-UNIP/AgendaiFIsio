# Histórico de consultas do paciente

O histórico usa os agendamentos já armazenados. Não precisa de nova tabela ou migration.

## Rota

`GET /api/paciente/{pacienteId}/historico-consultas`

É necessário enviar o token JWT. O próprio paciente pode consultar seu histórico. Um
profissional pode consultá-lo quando possui um agendamento não cancelado com esse paciente,
inclusive um atendimento futuro. Outros usuários recebem `403`; um identificador de paciente
inexistente recebe `404`.

Esta rota tem sua própria verificação de acesso. As rotas existentes de `api/agendamento`
continuam com as regras anteriores; revisar o acesso geral à agenda será uma tarefa separada
antes de usar dados clínicos reais.

A resposta é uma lista de agendamentos com `dataHora` até o momento da consulta, do mais recente
ao mais antigo. Cada item contém `agendamentoId`, `dataHora`, `profissionalId`,
`profissionalNome` e `status`. Agendamentos futuros não aparecem. Agendamentos passados
cancelados aparecem com status `Cancelado`, para que a interface não os apresente como consultas
realizadas. Um horário passado com status `Agendado` permanece com esse status: a API não presume
que o atendimento ocorreu.

O histórico reflete os status que já estão no banco. Ainda não existe rota para atualizar o
status de um agendamento após o atendimento; esse fluxo será necessário para marcar consultas
como `Concluido` no uso real. Avaliações e planos terapêuticos não integram esta resposta.

Exemplo de resposta:

```json
[
  {
    "agendamentoId": "bd66b8c6-a556-4aca-9bd4-15ec94f8a807",
    "dataHora": "2026-10-01T14:00:00",
    "profissionalId": "134f9b7a-0e24-42b8-92ef-835d8a58543d",
    "profissionalNome": "Ana Silva",
    "status": "Concluido"
  }
]
```

## Arquivos da implementação

- `DTOs/Paciente/HistoricoConsultaDTO.cs`: formato da resposta, sem dados pessoais ou observações.
- `Services/Paciente/IPacienteService.cs` e `PacienteService.cs`: autorização e consulta ao banco.
- `Controllers/PacienteController.cs`: rota HTTP e leitura da identidade autenticada.
- `Tests/Regression/Program.cs`: cenários de acesso, ordenação, status e exclusão de horários futuros.
