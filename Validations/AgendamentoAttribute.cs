using System.ComponentModel.DataAnnotations;
namespace AgendaiFisio.Validations;

public interface IAgendamentoValidavel
{
    Guid PacienteId { get; }
    Guid ProfissionalId { get; }
    DateOnly Data { get; }
    TimeOnly Hora { get; }
    string? Observacoes { get; }
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

        if (agendamento.PacienteId == Guid.Empty)
        {
            return new ValidationResult(
                "O paciente é obrigatório.",
                new[] { nameof(IAgendamentoValidavel.PacienteId) });
        }

        if (agendamento.ProfissionalId == Guid.Empty)
        {
            return new ValidationResult(
                "O profissional é obrigatório.",
                new[] { nameof(IAgendamentoValidavel.ProfissionalId) });
        }

        if (agendamento.Data == default && agendamento.Data <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return new ValidationResult(
                "A selecionada é inválida.",
                new[] { nameof(IAgendamentoValidavel.Data) });
        }

        if (agendamento.Hora ==default)
        {
            return new ValidationResult(
                "A horário desejado deve ser selecionado.",
                new[] { nameof(IAgendamentoValidavel.Hora) });
        }
        
        return ValidationResult.Success;
    }
}
