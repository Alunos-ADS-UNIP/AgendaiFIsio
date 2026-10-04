using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Constants;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações relacionadas ao perfil do paciente.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PacienteController : ControllerBase
    {
        private readonly IPacienteService _pacienteService;

        // Guarda o serviço usado para alterar o paciente.
        public PacienteController(IPacienteService pacienteService)
        {
            _pacienteService = pacienteService;
        }

        // Atualiza os dados do paciente que está logado.
        [HttpPut("completar-perfil")]
        [Authorize(Roles = "Paciente")] 
        public async Task<IActionResult> UpdatePacienteAsync([FromBody] PacienteUpdateDTO dto)
        {
            try
            {
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(usuarioIdClaim))
                {
                    // Impede a atualização sem identificar o usuário.
                    return Unauthorized("Usuário não identificado no token.");
                }

                if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
                {
                    return Unauthorized("Identificador de usuário inválido no token.");
                }

                await _pacienteService.UpdatePacienteAsync(usuarioId, dto);

                return Ok(new { mensagem = "Perfil atualizado com sucesso!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                // Devolve ao cliente o erro ocorrido no processamento.
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Exibe consultas passadas para o próprio paciente ou para o fisioterapeuta que tenha
        // um agendamento não cancelado com ele (inclusive o atendimento futuro em preparação).
        [HttpGet("{pacienteId:guid}/historico-consultas")]
        [Authorize(Roles = PerfilDeUsuario.Paciente + "," + PerfilDeUsuario.Profissional)]
        public async Task<ActionResult<IReadOnlyList<HistoricoConsultaDTO>>> ListarHistoricoConsultasAsync(
            Guid pacienteId)
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var tipoUsuario = User.IsInRole(PerfilDeUsuario.Paciente)
                ? PerfilDeUsuario.Paciente
                : PerfilDeUsuario.Profissional;

            try
            {
                var historico = await _pacienteService.ListarHistoricoConsultasAsync(
                    pacienteId, usuarioId, tipoUsuario);
                return Ok(historico);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
