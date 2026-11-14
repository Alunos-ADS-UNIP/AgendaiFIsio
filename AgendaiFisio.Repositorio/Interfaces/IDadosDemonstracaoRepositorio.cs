namespace AgendaiFisio.Repositorio.Interfaces;

public sealed record ContaDemonstracaoExpirada(Guid UsuarioId, string? FotoPerfilChave);

public interface IDadosDemonstracaoRepositorio
{
    Task<IReadOnlyList<ContaDemonstracaoExpirada>> ListarExpiradasAsync(
        DateTime agoraUtc,
        int limite,
        CancellationToken cancellationToken = default);

    Task<int> ExcluirContasAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        CancellationToken cancellationToken = default);
}
