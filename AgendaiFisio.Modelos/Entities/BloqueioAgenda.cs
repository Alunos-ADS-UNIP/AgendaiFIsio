namespace AgendaiFisio.Entities;

// Representa um intervalo indisponível na agenda de um profissional.
public class BloqueioAgenda
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfissionalId { get; set; }
    public virtual Profissional? Profissional { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
