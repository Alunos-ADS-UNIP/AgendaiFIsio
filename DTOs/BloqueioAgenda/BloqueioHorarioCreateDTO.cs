using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.BloqueioAgenda;

public sealed class BloqueioHorarioCreateDTO
{
    [Required(ErrorMessage = "A data e hora são obrigatórias.")]
    [JsonConverter(typeof(DataHoraUtcJsonConverter))]
    public DateTime DataHora { get; set; }

    [StringLength(500, ErrorMessage = "O motivo deve ter no máximo 500 caracteres.")]
    public string? Motivo { get; set; }
}
