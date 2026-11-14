using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IAgendaiFisioApiClient _api;

    public HomeController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<EspecialidadeResponseDTO> specialties = [];
        try
        {
            specialties = await _api.GetAsync<List<EspecialidadeResponseDTO>>(
                "/api/especialidade", authenticated: false, cancellationToken);
        }
        catch (Exception)
        {
            ViewData["CatalogUnavailable"] = true;
        }

        return View(new HomeViewModel { Specialties = specialties.Take(6).ToList() });
    }

    [HttpGet("/erro")]
    public IActionResult Error() => View();
}
