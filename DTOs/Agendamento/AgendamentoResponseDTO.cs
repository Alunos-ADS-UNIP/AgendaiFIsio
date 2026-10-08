namespace AgendaiFisio.DTOs.Agendamento;

// Dados completos devolvidos ao consultar ou listar um agendamento.
public class AgendamentoResponseDTO
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;

    public Guid ProfissionalId { get; set; }
    public string ProfissionalNome { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}
