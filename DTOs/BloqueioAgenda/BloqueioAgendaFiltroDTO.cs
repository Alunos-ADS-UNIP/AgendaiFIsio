using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.BloqueioAgenda;

public sealed class BloqueioAgendaFiltroDTO
{
    [Required(ErrorMessage = "O início do período é obrigatório.")]
    public DateTimeOffset? Inicio { get; set; }

    [Required(ErrorMessage = "O fim do período é obrigatório.")]
    public DateTimeOffset? Fim { get; set; }
}
