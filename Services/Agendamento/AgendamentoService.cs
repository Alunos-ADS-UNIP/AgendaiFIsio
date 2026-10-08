using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Constants;
using Microsoft.Data.SqlClient;

namespace AgendaiFisio.Services.Agendamento;

// Centraliza as regras de consulta e criação de agendamentos.
public class AgendamentoService : IAgendamentoService
{
    private readonly AgendaiFisioDbContext _context;

    public AgendamentoService(AgendaiFisioDbContext context)
    {
        _context = context;
    }

    // Retorna um agendamento com os dados de paciente e profissional.
    public async Task<Entities.Agendamento?> GetByIdAsync(Guid id, Guid usuarioId, string tipoUsuario)
    {
        return await VisiveisPara(usuarioId, tipoUsuario)
            .Include(a => a.Paciente)
            .Include(a => a.Profissional)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    // Lista a agenda, permitindo filtrar por dia, profissional e status.
    public async Task<IReadOnlyList<Entities.Agendamento>> ListAsync(
        Guid usuarioId,
        string tipoUsuario,
        DateTime? data = null,
        Guid? profissionalId = null,
        string? status = null)
    {
        IQueryable<Entities.Agendamento> query = VisiveisPara(usuarioId, tipoUsuario)
            .Include(a => a.Paciente)
            .Include(a => a.Profissional);

        if (data.HasValue)
        {
            var inicioDoDia = data.Value.Date;
            var fimDoDia = inicioDoDia.AddDays(1);
            query = query.Where(a => a.DataHora >= inicioDoDia && a.DataHora < fimDoDia);
        }

        if (profissionalId.HasValue)
            query = query.Where(a => a.ProfissionalId == profissionalId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusNormalizado = status.Trim();
            query = query.Where(a => a.Status == statusNormalizado);
        }

        return await query
            .OrderBy(a => a.DataHora)
            .ToListAsync();
    }

    // Cria um agendamento usando exclusivamente o perfil assinado no token do paciente.
    public Task<Entities.Agendamento> CreateForPatientAsync(
        Entities.Agendamento agendamento,
        Guid pacienteId)
    {
        agendamento.PacienteId = pacienteId;
        return ValidateAndCreateAsync(agendamento);
    }

    // Fluxo separado para a Clínica operar em nome de um paciente explícito.
    public Task<Entities.Agendamento> CreateForAdminAsync(Entities.Agendamento agendamento)
        => ValidateAndCreateAsync(agendamento);

    // Confere as entidades envolvidas e reserva o horário quando ele está disponível.
    private async Task<Entities.Agendamento> ValidateAndCreateAsync(
        Entities.Agendamento agendamento)
    {

        if (agendamento.DataHora.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data e hora do agendamento devem estar em UTC.");

        var pacienteExiste = await _context.Pacientes
            .AnyAsync(p => p.Id == agendamento.PacienteId);

        if (!pacienteExiste)
            throw new KeyNotFoundException("Paciente não encontrado.");

        var profissionalAtivo = await _context.Profissionais
            .AnyAsync(p => p.Id == agendamento.ProfissionalId && p.Ativo);

        if (!profissionalAtivo)
            throw new KeyNotFoundException("Profissional não encontrado ou inativo.");

        var horarioOcupado = await _context.Agendamentos.AnyAsync(a =>
            a.ProfissionalId == agendamento.ProfissionalId &&
            a.DataHora == agendamento.DataHora &&
            a.Status != StatusAgendamento.Cancelado);

        if (horarioOcupado)
            throw new InvalidOperationException("O profissional já possui um agendamento nesse horário.");

        agendamento.Status = NormalizarStatus(agendamento.Status);

        _context.Agendamentos.Add(agendamento);
        await _context.SaveChangesAsync();

        return agendamento;
    }

    // Altera a data somente quando o usuário pode acessar a consulta e o novo horário está livre.
    public async Task<Entities.Agendamento> ReagendarAsync(
        Guid id,
        DateTime novaDataHora,
        Guid usuarioId,
        string tipoUsuario)
    {
        if (novaDataHora.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A nova data e hora devem estar em UTC.");

        if (novaDataHora <= DateTime.UtcNow)
            throw new ArgumentException("O reagendamento deve ser marcado para uma data e hora futuras.");

        var agendamento = await AplicarVisibilidade(
                _context.Agendamentos
                    .Include(a => a.Paciente)
                    .Include(a => a.Profissional),
                usuarioId,
                tipoUsuario)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento is null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        if (StatusIgual(agendamento.Status, StatusAgendamento.Cancelado) ||
            StatusIgual(agendamento.Status, StatusAgendamento.Concluido))
            throw new InvalidOperationException(
                "Agendamentos cancelados ou concluídos não podem ser reagendados.");

        if (agendamento.DataHora == novaDataHora)
            return agendamento;

        if (agendamento.Profissional is null || !agendamento.Profissional.Ativo)
            throw new InvalidOperationException("O profissional responsável está inativo.");

        var horarioOcupado = await _context.Agendamentos.AnyAsync(a =>
            a.Id != id &&
            a.ProfissionalId == agendamento.ProfissionalId &&
            a.DataHora == novaDataHora &&
            a.Status != StatusAgendamento.Cancelado);

        if (horarioOcupado)
            throw new InvalidOperationException(
                "O profissional já possui um agendamento nesse horário.");

        agendamento.DataHora = novaDataHora;
        await SalvarTratandoConflitoDeHorarioAsync();
        return agendamento;
    }

    // Preserva o registro no histórico e muda somente seu estado para cancelado.
    public async Task<Entities.Agendamento> CancelarAsync(
        Guid id,
        Guid usuarioId,
        string tipoUsuario)
    {
        var agendamento = await AplicarVisibilidade(
                _context.Agendamentos
                    .Include(a => a.Paciente)
                    .Include(a => a.Profissional),
                usuarioId,
                tipoUsuario)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento is null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        if (StatusIgual(agendamento.Status, StatusAgendamento.Cancelado))
            throw new InvalidOperationException("O agendamento já está cancelado.");

        if (StatusIgual(agendamento.Status, StatusAgendamento.Concluido))
            throw new InvalidOperationException("Agendamentos concluídos não podem ser cancelados.");

        agendamento.Status = StatusAgendamento.Cancelado;
        await _context.SaveChangesAsync();
        return agendamento;
    }

    private async Task SalvarTratandoConflitoDeHorarioAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                "O profissional já possui um agendamento nesse horário.", ex);
        }
    }

    private static string NormalizarStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return StatusAgendamento.Agendado;

        var valor = status.Trim();
        if (StatusIgual(valor, StatusAgendamento.Agendado)) return StatusAgendamento.Agendado;
        if (StatusIgual(valor, StatusAgendamento.Confirmado)) return StatusAgendamento.Confirmado;
        if (StatusIgual(valor, StatusAgendamento.Cancelado)) return StatusAgendamento.Cancelado;
        if (StatusIgual(valor, StatusAgendamento.Concluido)) return StatusAgendamento.Concluido;
        return valor;
    }

    private static bool StatusIgual(string atual, string esperado) =>
        string.Equals(atual, esperado, StringComparison.OrdinalIgnoreCase);

    private IQueryable<Entities.Agendamento> VisiveisPara(Guid usuarioId, string tipoUsuario)
    {
        return AplicarVisibilidade(
            _context.Agendamentos.AsNoTracking(),
            usuarioId,
            tipoUsuario);
    }

    private static IQueryable<Entities.Agendamento> AplicarVisibilidade(
        IQueryable<Entities.Agendamento> query,
        Guid usuarioId,
        string tipoUsuario)
    {
        return tipoUsuario switch
        {
            PerfilDeUsuario.Paciente => query.Where(a => a.Paciente!.UsuarioId == usuarioId),
            PerfilDeUsuario.Profissional => query.Where(a => a.Profissional!.UsuarioId == usuarioId),
            PerfilDeUsuario.Admin => query,
            _ => query.Where(_ => false)
        };
    }
}
