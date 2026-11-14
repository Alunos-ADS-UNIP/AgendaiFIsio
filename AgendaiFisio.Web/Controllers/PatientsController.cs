using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Profissional")]
[Route("pacientes")]
public sealed class PatientsController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public PatientsController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = $"/api/paciente?pagina={Math.Max(page, 1)}&tamanhoPagina=12";
            if (!string.IsNullOrWhiteSpace(search)) path += $"&busca={Uri.EscapeDataString(search.Trim())}";
            var results = await _api.GetAsync<PagedResultDTO<PacienteListItemDTO>>(path, true, cancellationToken);
            return View(new PatientDirectoryViewModel { Search = search, Results = results });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var patientTask = _api.GetAsync<PacienteMeResponseDTO>($"/api/paciente/{id}", true, cancellationToken);
            var historyTask = _api.GetAsync<PagedResultDTO<HistoricoConsultaDTO>>(
                $"/api/paciente/{id}/historico-consultas?pagina=1&tamanhoPagina=20", true, cancellationToken);
            await Task.WhenAll(patientTask, historyTask);
            return View(new PatientDetailsViewModel { Patient = patientTask.Result, History = historyTask.Result });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }
}
