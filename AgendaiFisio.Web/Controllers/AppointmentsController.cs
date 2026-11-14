using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Paciente,Profissional")]
[Route("agendamentos")]
public sealed class AppointmentsController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public AppointmentsController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, DateTime? date, CancellationToken cancellationToken)
    {
        try
        {
            var query = "/api/agendamento?";
            if (!string.IsNullOrWhiteSpace(status)) query += $"status={Uri.EscapeDataString(status)}&";
            if (date.HasValue) query += $"data={date.Value:yyyy-MM-dd}&";
            var appointments = await _api.GetAsync<List<AgendamentoResponseDTO>>(query.TrimEnd('?', '&'), true, cancellationToken);
            return View(new AppointmentListViewModel { Appointments = appointments, Status = status, Date = date });
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
            return View(await _api.GetAsync<AgendamentoResponseDTO>($"/api/agendamento/{id}", true, cancellationToken));
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    [Authorize(Roles = "Paciente")]
    [HttpGet("novo")]
    public async Task<IActionResult> Create(Guid? professionalId, CancellationToken cancellationToken)
    {
        var model = new AppointmentCreateViewModel
        {
            ProfessionalId = professionalId ?? Guid.Empty,
            LocalDateTime = DateTime.Today.AddDays(1).AddHours(9)
        };
        await LoadProfessionalsAsync(model, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "Paciente")]
    [HttpPost("novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentCreateViewModel model, CancellationToken cancellationToken)
    {
        if (model.LocalDateTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.LocalDateTime), "Escolha uma data e um horário futuros.");
        if (model.LocalDateTime.Minute != 0)
            ModelState.AddModelError(nameof(model.LocalDateTime), "As consultas começam em hora cheia.");

        if (!ModelState.IsValid)
        {
            await LoadProfessionalsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var created = await _api.PostAsync<AgendamentoCreateDTO, AgendamentoResponseDTO>(
                "/api/agendamento",
                new AgendamentoCreateDTO
                {
                    ProfissionalId = model.ProfessionalId,
                    DataHora = BrazilTime.ToUtc(model.LocalDateTime),
                    Status = "Agendado",
                    Observacoes = model.Notes?.Trim()
                },
                true,
                cancellationToken);
            TempData["Success"] = "Consulta agendada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            await LoadProfessionalsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet("{id:guid}/reagendar")]
    public async Task<IActionResult> Reschedule(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await _api.GetAsync<AgendamentoResponseDTO>($"/api/agendamento/{id}", true, cancellationToken);
            return View(new RescheduleViewModel
            {
                AppointmentId = id,
                LocalDateTime = BrazilTime.ToLocal(appointment.DataHora)
            });
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    [HttpPost("{id:guid}/reagendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleViewModel model, CancellationToken cancellationToken)
    {
        model.AppointmentId = id;
        if (model.LocalDateTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.LocalDateTime), "Escolha uma data e um horário futuros.");
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PutAsync<ReagendamentoDTO, AgendamentoResponseDTO>(
                $"/api/agendamento/{id}/reagendar",
                new ReagendamentoDTO { DataHora = BrazilTime.ToUtc(model.LocalDateTime) },
                cancellationToken);
            TempData["Success"] = "Consulta reagendada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            return View(model);
        }
    }

    [HttpPost("{id:guid}/cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _api.PatchAsync<AgendamentoResponseDTO>($"/api/agendamento/{id}/cancelar", cancellationToken);
            TempData["Success"] = "Consulta cancelada. O registro foi mantido no histórico.";
        }
        catch (ApiRequestException exception)
        {
            TempData["Error"] = exception.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Paciente")]
    [HttpGet("indisponibilidade/{professionalId:guid}")]
    public async Task<IActionResult> Unavailability(Guid professionalId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        try
        {
            var startUtc = BrazilTime.ToUtc(start);
            var endUtc = BrazilTime.ToUtc(end);
            var path = $"/api/profissional/{professionalId}/indisponibilidade?inicio={Uri.EscapeDataString(startUtc.ToString("O"))}&fim={Uri.EscapeDataString(endUtc.ToString("O"))}";
            var values = await _api.GetAsync<List<IndisponibilidadeDTO>>(path, true, cancellationToken);
            return Json(values.Select(item => new
            {
                inicio = BrazilTime.ToLocal(item.Inicio).ToString("yyyy-MM-ddTHH:mm"),
                fim = BrazilTime.ToLocal(item.Fim).ToString("yyyy-MM-ddTHH:mm")
            }));
        }
        catch (ApiRequestException exception)
        {
            return StatusCode((int)exception.StatusCode, new { erro = exception.Message });
        }
    }

    private async Task LoadProfessionalsAsync(AppointmentCreateViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _api.GetAsync<PagedResultDTO<ProfissionalListItemDTO>>(
                "/api/profissional?ativo=true&pagina=1&tamanhoPagina=50", true, cancellationToken);
            model.Professionals = result.Itens;
        }
        catch (ApiRequestException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Professionals = [];
        }
    }
}
