using AgendaiFisio.Constants;
using AgendaiFisio.DTOs.BloqueioAgenda;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Services.BloqueioAgenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Controllers;

[ApiController]
[Route("api/agenda/bloqueios")]
[Authorize(Roles = PerfilDeUsuario.Profissional)]
public sealed class BloqueioAgendaController : ControllerBase
{
    private readonly IBloqueioAgendaService _bloqueioAgendaService;
    private readonly IUsuarioAtualService _usuarioAtualService;

    public BloqueioAgendaController(
        IBloqueioAgendaService bloqueioAgendaService,
        IUsuarioAtualService usuarioAtualService)
    {
        _bloqueioAgendaService = bloqueioAgendaService;
        _usuarioAtualService = usuarioAtualService;
    }

    [HttpGet("{id:guid}", Name = "ObterBloqueioAgenda")]
    public async Task<ActionResult<BloqueioAgendaResponseDTO>> ObterAsync(Guid id)
    {
        if (!TryObterProfissionalId(out var profissionalId))
            return Unauthorized(new { erro = "Profissional não identificado no token." });

        var bloqueio = await _bloqueioAgendaService.ObterAsync(id, profissionalId);
        return bloqueio is null
            ? NotFound(new { erro = "Bloqueio não encontrado." })
            : Ok(ToResponse(bloqueio));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BloqueioAgendaResponseDTO>>> ListarAsync(
        [FromQuery] DateTimeOffset? inicio,
        [FromQuery] DateTimeOffset? fim)
    {
        if (!TryObterProfissionalId(out var profissionalId))
            return Unauthorized(new { erro = "Profissional não identificado no token." });

        try
        {
            var bloqueios = await _bloqueioAgendaService.ListarAsync(
                profissionalId,
                inicio?.UtcDateTime,
                fim?.UtcDateTime);
            return Ok(bloqueios.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("horarios")]
    public async Task<ActionResult<BloqueioAgendaResponseDTO>> CriarHorarioAsync(
        [FromBody] BloqueioHorarioCreateDTO dto)
    {
        if (!TryObterProfissionalId(out var profissionalId))
            return Unauthorized(new { erro = "Profissional não identificado no token." });

        return await CriarAsync(() => _bloqueioAgendaService.CriarHorarioAsync(
            profissionalId,
            dto.DataHora,
            dto.Motivo));
    }

    [HttpPost("dias")]
    public async Task<ActionResult<BloqueioAgendaResponseDTO>> CriarDiaCompletoAsync(
        [FromBody] BloqueioDiaCreateDTO dto)
    {
        if (!TryObterProfissionalId(out var profissionalId))
            return Unauthorized(new { erro = "Profissional não identificado no token." });

        return await CriarAsync(() => _bloqueioAgendaService.CriarDiaCompletoAsync(
            profissionalId,
            dto.Data,
            dto.Motivo));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> ExcluirAsync(Guid id)
    {
        if (!TryObterProfissionalId(out var profissionalId))
            return Unauthorized(new { erro = "Profissional não identificado no token." });

        var removido = await _bloqueioAgendaService.ExcluirAsync(id, profissionalId);
        return removido
            ? NoContent()
            : NotFound(new { erro = "Bloqueio não encontrado." });
    }

    private async Task<ActionResult<BloqueioAgendaResponseDTO>> CriarAsync(
        Func<Task<Entities.BloqueioAgenda>> criar)
    {
        try
        {
            var bloqueio = await criar();
            return CreatedAtRoute(
                "ObterBloqueioAgenda",
                new { id = bloqueio.Id },
                ToResponse(bloqueio));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { erro = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    private bool TryObterProfissionalId(out Guid profissionalId)
    {
        var usuarioAtual = _usuarioAtualService.Obter();
        if (usuarioAtual?.TipoUsuario == PerfilDeUsuario.Profissional &&
            usuarioAtual.PerfilId is Guid perfilId)
        {
            profissionalId = perfilId;
            return true;
        }

        profissionalId = Guid.Empty;
        return false;
    }

    private static BloqueioAgendaResponseDTO ToResponse(Entities.BloqueioAgenda bloqueio) => new()
    {
        Id = bloqueio.Id,
        Inicio = DateTime.SpecifyKind(bloqueio.Inicio, DateTimeKind.Utc),
        Fim = DateTime.SpecifyKind(bloqueio.Fim, DateTimeKind.Utc),
        Tipo = bloqueio.Tipo,
        Motivo = bloqueio.Motivo,
        CriadoEm = DateTime.SpecifyKind(bloqueio.CriadoEm, DateTimeKind.Utc)
    };
}
