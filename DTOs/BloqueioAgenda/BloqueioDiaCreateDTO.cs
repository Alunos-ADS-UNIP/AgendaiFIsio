using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.BloqueioAgenda;

public sealed class BloqueioDiaCreateDTO
{
    public DateOnly Data { get; set; }

    [StringLength(500, ErrorMessage = "O motivo deve ter no máximo 500 caracteres.")]
    public string? Motivo { get; set; }
}
