namespace AgendaiFisio.DTOs.Agendamento;

// Filtros opcionais usados na consulta da agenda.
public class AgendamentoFilterDTO
{
    public DateTime? Data { get; set; }
    public Guid? ProfissionalId { get; set; }
    public string? Status { get; set; }
}
