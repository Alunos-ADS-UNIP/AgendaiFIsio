using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Constants;

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
            a.Status != "Cancelado");

        if (horarioOcupado)
            throw new InvalidOperationException("O profissional já possui um agendamento nesse horário.");

        if (string.IsNullOrWhiteSpace(agendamento.Status))
            agendamento.Status = "Agendado";

        _context.Agendamentos.Add(agendamento);
        await _context.SaveChangesAsync();

        return agendamento;
    }

    private IQueryable<Entities.Agendamento> VisiveisPara(Guid usuarioId, string tipoUsuario)
    {
        var query = _context.Agendamentos.AsNoTracking();
        return tipoUsuario switch
        {
            PerfilDeUsuario.Paciente => query.Where(a => a.Paciente!.UsuarioId == usuarioId),
            PerfilDeUsuario.Profissional => query.Where(a => a.Profissional!.UsuarioId == usuarioId),
            PerfilDeUsuario.Admin => query,
            _ => query.Where(_ => false)
        };
    }
}
