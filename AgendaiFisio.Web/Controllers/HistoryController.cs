using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Paciente")]
[Route("historico")]
public sealed class HistoryController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public HistoryController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _api.GetAsync<PagedResultDTO<HistoricoConsultaDTO>>(
                $"/api/paciente/me/historico-consultas?pagina={Math.Max(page, 1)}&tamanhoPagina=10",
                true,
                cancellationToken);
            return View(new HistoryViewModel { History = history, Page = Math.Max(page, 1) });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }
}
