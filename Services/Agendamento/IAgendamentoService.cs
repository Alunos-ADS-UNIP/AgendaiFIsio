using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.Agendamento;

public interface IAgendamentoService
{
    Task<Entities.Agendamento?> GetByIdAsync(Guid id, Guid usuarioId, string tipoUsuario);

    Task<IReadOnlyList<Entities.Agendamento>> ListAsync(
        Guid usuarioId,
        string tipoUsuario,
        DateTime? data = null,
        Guid? profissionalId = null,
        string? status = null);

    Task<Entities.Agendamento> CreateForPatientAsync(
        Entities.Agendamento agendamento,
        Guid pacienteId);

    Task<Entities.Agendamento> CreateForAdminAsync(Entities.Agendamento agendamento);
}
