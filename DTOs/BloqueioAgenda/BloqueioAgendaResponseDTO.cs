namespace AgendaiFisio.DTOs.BloqueioAgenda;

public sealed class BloqueioAgendaResponseDTO
{
    public Guid Id { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; }
}
