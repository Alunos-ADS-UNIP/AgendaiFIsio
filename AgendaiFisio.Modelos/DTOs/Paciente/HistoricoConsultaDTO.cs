namespace AgendaiFisio.DTOs.Paciente;

// Dados de um agendamento passado exibidos no histórico do paciente.
public class HistoricoConsultaDTO
{
    public Guid AgendamentoId { get; set; }
    public DateTime DataHora { get; set; }
    public Guid ProfissionalId { get; set; }
    public string ProfissionalNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
