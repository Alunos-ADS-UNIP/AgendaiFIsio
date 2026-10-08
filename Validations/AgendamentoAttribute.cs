using System.ComponentModel.DataAnnotations;
namespace AgendaiFisio.Validations;

public interface IAgendamentoValidavel
{
    Guid ProfissionalId { get; }
    DateTime DataHora { get; }
    string? Status { get; }
}

public interface IAgendamentoComPaciente
{
    Guid PacienteId { get; }
}

// Valida os dados que podem ser conferidos sem consultar o banco de dados.
[AttributeUsage(AttributeTargets.Class)]
public class AgendamentoAttribute : ValidationAttribute
{
    private static readonly HashSet<string> StatusPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Agendado",
        "Confirmado",
        "Cancelado",
        "Concluido"
    };

    public AgendamentoAttribute()
    {
        ErrorMessage = "Os dados do agendamento são inválidos.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IAgendamentoValidavel agendamento)
            return new ValidationResult(ErrorMessage);

        if (value is IAgendamentoComPaciente agendamentoComPaciente &&
            agendamentoComPaciente.PacienteId == Guid.Empty)
        {
            return new ValidationResult(
                "O paciente é obrigatório.",
                new[] { nameof(IAgendamentoComPaciente.PacienteId) });
        }

        if (agendamento.ProfissionalId == Guid.Empty)
        {
            return new ValidationResult(
                "O profissional é obrigatório.",
                new[] { nameof(IAgendamentoValidavel.ProfissionalId) });
        }

        if (agendamento.DataHora == default)
        {
            return new ValidationResult(
                "A data e hora do agendamento são obrigatórias.",
                new[] { nameof(IAgendamentoValidavel.DataHora) });
        }

        if (agendamento.DataHora <= DateTime.UtcNow)
        {
            return new ValidationResult(
                "O agendamento deve ser marcado para uma data e hora futuras.",
                new[] { nameof(IAgendamentoValidavel.DataHora) });
        }

        if (!string.IsNullOrWhiteSpace(agendamento.Status) &&
            !StatusPermitidos.Contains(agendamento.Status.Trim()))
        {
            return new ValidationResult(
                "O status deve ser Agendado, Confirmado, Cancelado ou Concluido.",
                new[] { nameof(IAgendamentoValidavel.Status) });
        }

        return ValidationResult.Success;
    }
}
