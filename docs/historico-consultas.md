# Histórico de consultas do paciente

O histórico usa os agendamentos já armazenados. A migration
`IndiceHistoricoPacienteDataHora` acrescenta um índice composto para a consulta por paciente e
data; não cria tabela nem transforma os dados existentes.

## Rota e paginação

`GET /api/paciente/{pacienteId}/historico-consultas?pagina=1&tamanhoPagina=10`

É necessário enviar um token JWT. `pagina` começa em 1 e `tamanhoPagina` aceita no máximo 50.
Valores menores que 1 usam os padrões 1 e 10. A resposta contém `itens`, `totalRegistros`,
`paginaAtual`, `tamanhoPagina` e `totalPaginas`. Uma página além do fim tem `itens: []` e preserva
o total. Os itens são ordenados por data decrescente e, em caso de empate, por identificador
decrescente.

O próprio paciente pode consultar seu histórico. Um profissional pode consultá-lo se houver um
agendamento não cancelado entre ambos, inclusive futuro. Paciente inexistente e paciente
inacessível recebem o mesmo `404`, sem revelar se o identificador existe. A conta `Clinica`
recebe `403` nesta rota.

Exemplo de resposta:

```json
{
  "itens": [
    {
      "agendamentoId": "bd66b8c6-a556-4aca-9bd4-15ec94f8a807",
      "dataHora": "2026-10-01T17:00:00Z",
      "profissionalId": "134f9b7a-0e24-42b8-92ef-835d8a58543d",
      "profissionalNome": "Ana Silva",
      "status": "Concluido"
    }
  ],
  "totalRegistros": 1,
  "paginaAtual": 1,
  "tamanhoPagina": 10,
  "totalPaginas": 1
}
```

## Quem cria e consulta agendamentos

`POST /api/agendamento` aceita somente o próprio paciente ou a clínica. O `pacienteId` do corpo
continua no contrato para compatibilidade, mas, quando o usuário é paciente, a API consulta o
perfil pelo JWT e rejeita um identificador diferente. A gravação usa o identificador obtido do
perfil autenticado. Profissionais não criam agendamentos para estabelecer acesso ao histórico.

`GET /api/agendamento` e `GET /api/agendamento/{id}` retornam somente agendamentos do próprio
paciente ou do próprio profissional. A clínica mantém acesso administrativo à agenda. Um
agendamento específico fora desse escopo responde `404`.

## Datas e status

O campo `dataHora` de `POST /api/agendamento` exige ISO 8601 com `Z` ou offset explícito. Exemplos:
`2026-10-04T17:00:00Z` e `2026-10-04T14:00:00-03:00` indicam o mesmo instante. A API converte
para UTC antes de gravar e devolve datas de agendamento e histórico com `Z`. Uma data sem offset
recebe `400`.

O SQL Server usa `datetime2` e não guarda o fuso. Registros antigos não contêm informação
suficiente para reconstruir um fuso original; a leitura os interpreta como UTC até uma revisão
dos dados legados. O histórico inclui horários até o instante atual em UTC e exclui os futuros.
Agendamentos passados cancelados aparecem com status `Cancelado`. Um horário passado com status
`Agendado` permanece com esse status: a API não presume que o atendimento ocorreu. Ainda não
existe rota para atualizar o status após o atendimento. Avaliações e planos terapêuticos não
integram esta resposta.

## Arquivos principais

- `DTOs/Paciente/HistoricoConsultaFiltroDTO.cs`: página e tamanho máximos.
- `Services/Paciente/PacienteService.cs`: autorização e consulta paginada.
- `Controllers/PacienteController.cs`: rota e identidade autenticada.
- `Controllers/AgendamentoController.cs` e `Services/Agendamento/`: escopo da agenda e criação.
- `Validations/DataHoraUtcJsonConverter.cs`: contrato de data com offset.
- `Tests/Regression/Program.cs`: testes de acesso, datas e paginação.
