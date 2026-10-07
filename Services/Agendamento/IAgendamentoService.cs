using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.Agendamento;

public interface IAgendamentoService
{
    Task<Entities.Agendamento?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<Entities.Agendamento>> ListAsync(
        DateOnly? data = null,
        Guid? profissionalId = null,
        string? status = null);

    Task<Entities.Agendamento> CreateAsync(Entities.Agendamento agendamento);
    Task<ReagendamentoDTO> UpdateAsync(ReagendamentoDTO agendamento);
    Task<string> DeleteAsync(CancelamentoDTO cancelamento);
    
}
