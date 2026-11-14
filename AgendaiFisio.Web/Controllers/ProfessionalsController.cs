using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Paciente")]
[Route("profissionais")]
public sealed class ProfessionalsController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public ProfessionalsController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, Guid? specialtyId, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"/api/profissional?ativo=true&pagina={Math.Max(page, 1)}&tamanhoPagina=12";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&nome={Uri.EscapeDataString(search.Trim())}";
            if (specialtyId.HasValue) query += $"&especialidadeId={specialtyId}";

            var resultsTask = _api.GetAsync<PagedResultDTO<ProfissionalListItemDTO>>(query, true, cancellationToken);
            var specialtiesTask = _api.GetAsync<List<EspecialidadeResponseDTO>>("/api/especialidade", false, cancellationToken);
            await Task.WhenAll(resultsTask, specialtiesTask);
            return View(new ProfessionalDirectoryViewModel
            {
                Search = search,
                SpecialtyId = specialtyId,
                Results = resultsTask.Result,
                Specialties = specialtiesTask.Result
            });
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
            return View(await _api.GetAsync<ProfissionalDetailDTO>($"/api/profissional/{id}", true, cancellationToken));
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }
}
