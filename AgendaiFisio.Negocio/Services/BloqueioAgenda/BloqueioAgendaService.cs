using AgendaiFisio.Constants;
using AgendaiFisio.Repositorio;
using AgendaiFisio.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaiFisio.Services.BloqueioAgenda;

public sealed class BloqueioAgendaService : IBloqueioAgendaService
{
    private const string FusoHorarioAgenda = "America/Sao_Paulo";
    private static readonly TimeSpan JanelaMaximaConsulta = TimeSpan.FromDays(93);
    private readonly IBloqueioAgendaRepositorio _bloqueios;
    private readonly IProfissionalRepositorio _profissionais;
    private readonly IAgendamentoRepositorio _agendamentos;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public BloqueioAgendaService(
        IBloqueioAgendaRepositorio bloqueios,
        IProfissionalRepositorio profissionais,
        IAgendamentoRepositorio agendamentos,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _bloqueios = bloqueios;
        _profissionais = profissionais;
        _agendamentos = agendamentos;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Task<Entities.BloqueioAgenda?> ObterAsync(Guid id, Guid profissionalId) =>
        _bloqueios.Consultar().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.ProfissionalId == profissionalId);

    public async Task<IReadOnlyList<Entities.BloqueioAgenda>> ListarAsync(
        Guid profissionalId,
        DateTime inicio,
        DateTime fim)
    {
        if (inicio.Kind != DateTimeKind.Utc || fim.Kind != DateTimeKind.Utc)
            throw new ArgumentException("O período deve estar em UTC.");
        if (fim <= inicio)
            throw new ArgumentException("O fim do período deve ser posterior ao início.");
        if (fim - inicio > JanelaMaximaConsulta)
            throw new ArgumentException("O período consultado deve ter no máximo 93 dias.");

        var query = _bloqueios.Consultar().AsNoTracking()
            .Where(b => b.ProfissionalId == profissionalId &&
                b.Fim > inicio &&
                b.Inicio < fim);

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
        var bloqueio = await _bloqueios.Consultar()
            .FirstOrDefaultAsync(b => b.Id == id && b.ProfissionalId == profissionalId);
        if (bloqueio is null)
            return false;

        _bloqueios.Remover(bloqueio);
        await _unidadeDeTrabalho.SalvarAsync();
        return true;
    }

    private async Task<Entities.BloqueioAgenda> CriarAsync(
        Guid profissionalId,
        DateTime inicio,
        DateTime fim,
        string tipo,
        string? motivo)
    {
        var profissionalAtivo = await _profissionais.Consultar().AsNoTracking()
            .AnyAsync(p => p.Id == profissionalId && p.Ativo);
        if (!profissionalAtivo)
            throw new KeyNotFoundException("Profissional não encontrado ou inativo.");

        var existeConsulta = await _agendamentos.Consultar().AsNoTracking().AnyAsync(a =>
            a.ProfissionalId == profissionalId &&
            a.Status != StatusAgendamento.Cancelado &&
            a.DataHora >= inicio &&
            a.DataHora < fim);
        if (existeConsulta)
            throw new InvalidOperationException(
                "O período possui uma consulta e não pode ser bloqueado.");

        var existeBloqueio = await _bloqueios.Consultar().AsNoTracking().AnyAsync(b =>
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

        _bloqueios.Adicionar(bloqueio);
        try
        {
            await _unidadeDeTrabalho.SalvarAsync();
        }
        catch (ConflitoRepositorioException ex) when (
            ex.Codigo is CodigoConflitoRepositorio.BloqueioComConsulta or
                CodigoConflitoRepositorio.BloqueioSobreposto)
        {
            var mensagem = ex.Codigo == CodigoConflitoRepositorio.BloqueioComConsulta
                ? "O período possui uma consulta e não pode ser bloqueado."
                : "O período informado já possui um bloqueio.";
            throw new InvalidOperationException(mensagem, ex);
        }

        return bloqueio;
    }
}
