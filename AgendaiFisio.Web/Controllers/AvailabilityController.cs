using AgendaiFisio.DTOs.BloqueioAgenda;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Profissional")]
[Route("disponibilidade")]
public sealed class AvailabilityController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public AvailabilityController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var start = DateTime.UtcNow.Date;
            var end = start.AddDays(93);
            var path = $"/api/agenda/bloqueios?inicio={Uri.EscapeDataString(start.ToString("O"))}&fim={Uri.EscapeDataString(end.ToString("O"))}";
            var blocks = await _api.GetAsync<List<BloqueioAgendaResponseDTO>>(path, true, cancellationToken);
            return View(new AvailabilityViewModel { Blocks = blocks });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    [HttpPost("dia")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockDay(AvailabilityViewModel model, CancellationToken cancellationToken)
    {
        if (model.Day.Date < DateTime.Today)
        {
            TempData["Error"] = "Escolha hoje ou uma data futura.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await _api.PostAsync(
                "/api/agenda/bloqueios/dias",
                new BloqueioDiaCreateDTO { Data = DateOnly.FromDateTime(model.Day), Motivo = model.Reason?.Trim() },
                true,
                cancellationToken);
            TempData["Success"] = "Dia bloqueado na sua agenda.";
        }
        catch (ApiRequestException exception) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("horario")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockTime(AvailabilityViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _api.PostAsync(
                "/api/agenda/bloqueios/horarios",
                new BloqueioHorarioCreateDTO
                {
                    DataHora = BrazilTime.ToUtc(model.LocalDateTime),
                    Motivo = model.Reason?.Trim()
                },
                true,
                cancellationToken);
            TempData["Success"] = "Horário bloqueado na sua agenda.";
        }
        catch (ApiRequestException exception) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _api.DeleteAsync($"/api/agenda/bloqueios/{id}", cancellationToken);
            TempData["Success"] = "Bloqueio removido.";
        }
        catch (ApiRequestException exception) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }
}
