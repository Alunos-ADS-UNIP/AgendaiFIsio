using AgendaiFisio.Validations;

namespace AgendaiFisio.Entities;

// Representa um horário reservado na agenda de um profissional.
[Agendamento]
public class Agendamento : IAgendamentoValidavel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PacienteId { get; set; }
    public virtual Paciente? Paciente { get; set; }

    public Guid ProfissionalId { get; set; }
    public virtual Profissional? Profissional { get; set; }

    public DateTime Data { get; set; }
    public DateTime Hora { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}
