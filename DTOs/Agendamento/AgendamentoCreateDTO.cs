using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Validations;
using System.Text.Json.Serialization;

namespace AgendaiFisio.DTOs.Agendamento;

// Dados recebidos ao reservar um horário.
[Agendamento]
public class AgendamentoCreateDTO : IAgendamentoValidavel
{
    [Required(ErrorMessage = "O profissional é obrigatório.")]
    public Guid ProfissionalId { get; set; }

    [Required(ErrorMessage = "A data e hora do agendamento são obrigatórias.")]
    [JsonConverter(typeof(DataHoraUtcJsonConverter))]
    public DateTime DataHora { get; set; }

    [StringLength(30, ErrorMessage = "O status deve ter no máximo 30 caracteres.")]
    public string? Status { get; set; }

    [StringLength(1000, ErrorMessage = "As observações devem ter no máximo 1000 caracteres.")]
    public string? Observacoes { get; set; }
}
