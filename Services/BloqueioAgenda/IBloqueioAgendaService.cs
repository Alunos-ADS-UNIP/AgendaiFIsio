using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.BloqueioAgenda;

public interface IBloqueioAgendaService
{
    Task<Entities.BloqueioAgenda?> ObterAsync(Guid id, Guid profissionalId);

    Task<IReadOnlyList<Entities.BloqueioAgenda>> ListarAsync(
        Guid profissionalId,
        DateTime inicio,
        DateTime fim);

    Task<Entities.BloqueioAgenda> CriarHorarioAsync(
        Guid profissionalId,
        DateTime dataHora,
        string? motivo);

    Task<Entities.BloqueioAgenda> CriarDiaCompletoAsync(
        Guid profissionalId,
        DateOnly data,
        string? motivo);

    Task<bool> ExcluirAsync(Guid id, Guid profissionalId);
}
