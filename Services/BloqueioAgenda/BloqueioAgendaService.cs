using AgendaiFisio.Constants;
using AgendaiFisio.Context;
using Microsoft.EntityFrameworkCore;

namespace AgendaiFisio.Services.BloqueioAgenda;

public sealed class BloqueioAgendaService : IBloqueioAgendaService
{
    private const string FusoHorarioAgenda = "America/Sao_Paulo";
    private readonly AgendaiFisioDbContext _context;

    public BloqueioAgendaService(AgendaiFisioDbContext context)
    {
        _context = context;
    }

    public Task<Entities.BloqueioAgenda?> ObterAsync(Guid id, Guid profissionalId) =>
        _context.BloqueiosAgenda.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.ProfissionalId == profissionalId);

    public async Task<IReadOnlyList<Entities.BloqueioAgenda>> ListarAsync(
        Guid profissionalId,
        DateTime? inicio,
        DateTime? fim)
    {
        if (inicio.HasValue && fim.HasValue && fim <= inicio)
            throw new ArgumentException("O fim do período deve ser posterior ao início.");

        var query = _context.BloqueiosAgenda.AsNoTracking()
            .Where(b => b.ProfissionalId == profissionalId);

        if (inicio.HasValue)
            query = query.Where(b => b.Fim > inicio.Value);
        if (fim.HasValue)
            query = query.Where(b => b.Inicio < fim.Value);

        return await query.OrderBy(b => b.Inicio).ToListAsync();
    }

    public Task<Entities.BloqueioAgenda> CriarHorarioAsync(
        Guid profissionalId,
        DateTime dataHora,
        string? motivo)
    {
        if (dataHora.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data e hora devem estar em UTC.");
        if (dataHora <= DateTime.UtcNow)
            throw new ArgumentException("Só é possível bloquear horários futuros.");
        if (dataHora.Minute != 0 || dataHora.Second != 0 || dataHora.Millisecond != 0)
            throw new ArgumentException("O bloqueio deve começar em uma hora cheia, como 09:00 ou 10:00.");

        return CriarAsync(
            profissionalId,
            dataHora,
            dataHora.AddHours(1),
            TipoBloqueioAgenda.Horario,
            motivo);
    }

    public Task<Entities.BloqueioAgenda> CriarDiaCompletoAsync(
        Guid profissionalId,
        DateOnly data,
        string? motivo)
    {
        if (data == default)
            throw new ArgumentException("A data é obrigatória.");

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(FusoHorarioAgenda);
        var inicioLocal = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fimLocal = data.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var inicio = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, fuso);
        var fim = TimeZoneInfo.ConvertTimeToUtc(fimLocal, fuso);

        if (fim <= DateTime.UtcNow)
            throw new ArgumentException("Só é possível bloquear o dia atual ou dias futuros.");

        return CriarAsync(
            profissionalId,
            inicio,
            fim,
            TipoBloqueioAgenda.DiaCompleto,
            motivo);
    }

    public async Task<bool> ExcluirAsync(Guid id, Guid profissionalId)
    {
        var bloqueio = await _context.BloqueiosAgenda
            .FirstOrDefaultAsync(b => b.Id == id && b.ProfissionalId == profissionalId);
        if (bloqueio is null)
            return false;

        _context.BloqueiosAgenda.Remove(bloqueio);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<Entities.BloqueioAgenda> CriarAsync(
        Guid profissionalId,
        DateTime inicio,
        DateTime fim,
        string tipo,
        string? motivo)
    {
        var profissionalAtivo = await _context.Profissionais.AsNoTracking()
            .AnyAsync(p => p.Id == profissionalId && p.Ativo);
        if (!profissionalAtivo)
            throw new KeyNotFoundException("Profissional não encontrado ou inativo.");

        var existeConsulta = await _context.Agendamentos.AsNoTracking().AnyAsync(a =>
            a.ProfissionalId == profissionalId &&
            a.Status != StatusAgendamento.Cancelado &&
            a.DataHora >= inicio &&
            a.DataHora < fim);
        if (existeConsulta)
            throw new InvalidOperationException(
                "O período possui uma consulta e não pode ser bloqueado.");

        var existeBloqueio = await _context.BloqueiosAgenda.AsNoTracking().AnyAsync(b =>
            b.ProfissionalId == profissionalId &&
            b.Inicio < fim &&
            b.Fim > inicio);
        if (existeBloqueio)
            throw new InvalidOperationException(
                "O período informado já possui um bloqueio.");

        var bloqueio = new Entities.BloqueioAgenda
        {
            ProfissionalId = profissionalId,
            Inicio = inicio,
            Fim = fim,
            Tipo = tipo,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        _context.BloqueiosAgenda.Add(bloqueio);
        await _context.SaveChangesAsync();
        return bloqueio;
    }
}
