using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Agendamento;

// Nova data e hora recebidas ao reagendar uma consulta existente.
public sealed class ReagendamentoDTO : IValidatableObject
{
    [Required(ErrorMessage = "A nova data e hora são obrigatórias.")]
    [JsonConverter(typeof(DataHoraUtcJsonConverter))]
    public DateTime DataHora { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataHora == default)
        {
            yield return new ValidationResult(
                "A nova data e hora são obrigatórias.",
                new[] { nameof(DataHora) });
            yield break;
        }

        if (DataHora.Kind != DateTimeKind.Utc)
        {
            yield return new ValidationResult(
                "A nova data e hora devem estar em UTC.",
                new[] { nameof(DataHora) });
        }

        if (DataHora <= DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "O reagendamento deve ser marcado para uma data e hora futuras.",
                new[] { nameof(DataHora) });
        }
    }
}
