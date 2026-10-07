using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Agendamento;

// Dados recebidos ao reservar um horário.
[Agendamento]
public class AgendamentoCreateDTO
{
    [Required(ErrorMessage = "O paciente é obrigatório.")]
    public Guid PacienteId { get; set; }

    [Required(ErrorMessage = "O profissional é obrigatório.")]
    public Guid ProfissionalId { get; set; }

    [Required(ErrorMessage = "A data do agendamento é obrigatória.")]
    public DateOnly Data { get; set; }
    
    [Required(ErrorMessage = "A hora do agendamento é obrigatória.")]
    public TimeOnly Hora { get; set; }
    
    [StringLength(1000, ErrorMessage = "As observações devem ter no máximo 1000 caracteres.")]
    public string? Observacoes { get; set; }
}
