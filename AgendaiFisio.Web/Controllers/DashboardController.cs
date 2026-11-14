using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Paciente,Profissional")]
[Route("painel")]
public sealed class DashboardController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public DashboardController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var role = User.IsInRole("Profissional") ? "Profissional" : "Paciente";
            var appointmentsTask = _api.GetAsync<List<AgendamentoResponseDTO>>("/api/agendamento", true, cancellationToken);

            if (role == "Profissional")
            {
                var profileTask = _api.GetAsync<ProfissionalMeDTO>("/api/profissional/me", true, cancellationToken);
                await Task.WhenAll(appointmentsTask, profileTask);
                return View(new DashboardViewModel
                {
                    Role = role,
                    DisplayName = profileTask.Result.NomeCompleto,
                    Appointments = appointmentsTask.Result,
                    TodayCount = CountToday(appointmentsTask.Result)
                });
            }

            var patientTask = _api.GetAsync<PacienteMeResponseDTO>("/api/paciente/me", true, cancellationToken);
            var professionalsTask = _api.GetAsync<PagedResultDTO<ProfissionalListItemDTO>>(
                "/api/profissional?ativo=true&pagina=1&tamanhoPagina=6", true, cancellationToken);
            var specialtiesTask = _api.GetAsync<List<EspecialidadeResponseDTO>>(
                "/api/especialidade", false, cancellationToken);
            await Task.WhenAll(appointmentsTask, patientTask, professionalsTask, specialtiesTask);

            return View(new DashboardViewModel
            {
                Role = role,
                DisplayName = patientTask.Result.NomeCompleto,
                Appointments = appointmentsTask.Result,
                Professionals = professionalsTask.Result.Itens,
                Specialties = specialtiesTask.Result.Take(4).ToList(),
                TodayCount = CountToday(appointmentsTask.Result)
            });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    private static int CountToday(IEnumerable<AgendamentoResponseDTO> appointments)
    {
        var today = DateTime.Today;
        return appointments.Count(item =>
        {
            var local = BrazilTime.ToLocal(item.DataHora);
            return local.Date == today && !item.Status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase);
        });
    }
}
