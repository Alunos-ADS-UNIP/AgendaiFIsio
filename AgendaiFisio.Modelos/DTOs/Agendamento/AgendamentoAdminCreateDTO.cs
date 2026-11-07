using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Agendamento;

// Contrato exclusivo da Clínica para agendar em nome de um paciente.
public sealed class AgendamentoAdminCreateDTO : AgendamentoCreateDTO, IAgendamentoComPaciente
{
    [Required(ErrorMessage = "O paciente é obrigatório.")]
    public Guid PacienteId { get; set; }
}
