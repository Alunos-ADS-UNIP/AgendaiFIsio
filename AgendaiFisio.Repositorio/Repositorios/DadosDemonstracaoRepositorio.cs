using AgendaiFisio.Context;
using AgendaiFisio.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaiFisio.Repositorio.Repositorios;

// Remove todas as relações operacionais antes da conta, respeitando as FKs Restrict.
public sealed class DadosDemonstracaoRepositorio : IDadosDemonstracaoRepositorio
{
    private readonly AgendaiFisioDbContext _db;

    public DadosDemonstracaoRepositorio(AgendaiFisioDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContaDemonstracaoExpirada>> ListarExpiradasAsync(
        DateTime agoraUtc,
        int limite,
        CancellationToken cancellationToken = default) =>
        await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.DadosExpiramEmUtc <= agoraUtc)
            .OrderBy(u => u.DadosExpiramEmUtc)
            .Take(limite)
            .Select(u => new ContaDemonstracaoExpirada(
                u.Id,
                u.Profissional == null ? null : u.Profissional.FotoPerfilChave))
            .ToListAsync(cancellationToken);

    public async Task<int> ExcluirContasAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        if (usuarioIds.Count == 0)
            return 0;

        var ids = usuarioIds.Distinct().ToArray();
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var pacientes = await _db.Pacientes
                .Where(p => ids.Contains(p.UsuarioId))
                .Select(p => new { p.Id, EnderecoId = p.Endereco.Id })
                .ToListAsync(cancellationToken);
            var profissionais = await _db.Profissionais
                .Where(p => ids.Contains(p.UsuarioId))
                .Select(p => new { p.Id, p.EnderecoId })
                .ToListAsync(cancellationToken);

            var pacienteIds = pacientes.Select(p => p.Id).ToArray();
            var profissionalIds = profissionais.Select(p => p.Id).ToArray();
            var enderecoIds = pacientes.Select(p => p.EnderecoId)
                .Concat(profissionais.Where(p => p.EnderecoId.HasValue).Select(p => p.EnderecoId!.Value))
                .Distinct()
                .ToArray();

            await _db.Agendamentos
                .Where(a => pacienteIds.Contains(a.PacienteId) ||
                            profissionalIds.Contains(a.ProfissionalId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.BloqueiosAgenda
                .Where(b => profissionalIds.Contains(b.ProfissionalId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Pacientes
                .Where(p => ids.Contains(p.UsuarioId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Profissionais
                .Where(p => ids.Contains(p.UsuarioId))
                .ExecuteDeleteAsync(cancellationToken);

            var excluidas = await _db.Usuarios
                .Where(u => ids.Contains(u.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await _db.Enderecos
                .Where(e => enderecoIds.Contains(e.Id) &&
                            !_db.Pacientes.Any(p => p.Endereco.Id == e.Id) &&
                            !_db.Profissionais.Any(p => p.EnderecoId == e.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return excluidas;
        });
    }
}
