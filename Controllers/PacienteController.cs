using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Constants;
using AgendaiFisio.DTOs;
using AgendaiFisio.Services.Auth;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações relacionadas ao perfil do paciente.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PacienteController : ControllerBase
    {
        private readonly IPacienteService _pacienteService;
        private readonly IUsuarioAtualService _usuarioAtualService;

        // Guarda o serviço usado para alterar o paciente.
        public PacienteController(
            IPacienteService pacienteService,
            IUsuarioAtualService usuarioAtualService)
        {
            _pacienteService = pacienteService;
            _usuarioAtualService = usuarioAtualService;
        }

        // Retorna o perfil ligado ao paciente autenticado.
        [HttpGet("me")]
        [Authorize(Roles = PerfilDeUsuario.Paciente)]
        public async Task<ActionResult<PacienteMeResponseDTO>> MeAsync()
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual?.PerfilId is not Guid pacienteId)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var paciente = await _pacienteService.ObterMeuPerfilAsync(pacienteId);
            if (paciente is null)
                return NotFound(new { erro = "Perfil de paciente não encontrado." });

            return Ok(paciente);
        }

        // Atualiza os dados do paciente que está logado.
        [HttpPut("completar-perfil")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> UpdatePacienteAsync([FromBody] PacienteUpdateDTO dto)
        {
            try
            {
                var usuarioAtual = _usuarioAtualService.Obter();
                if (usuarioAtual is null)
                    return Unauthorized("Usuário não identificado no token.");

                await _pacienteService.UpdatePacienteAsync(usuarioAtual.UsuarioId, dto);

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
        [Authorize(Roles = PerfilDeUsuario.Profissional)]
        public async Task<ActionResult<PagedResultDTO<HistoricoConsultaDTO>>> ListarHistoricoConsultasAsync(
            Guid pacienteId, [FromQuery] HistoricoConsultaFiltroDTO filtro)
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual is null)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            try
            {
                var historico = await _pacienteService.ListarHistoricoConsultasAsync(
                    pacienteId,
                    usuarioAtual.UsuarioId,
                    usuarioAtual.TipoUsuario,
                    filtro);
                return Ok(historico);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
        }

        // Exibe o histórico do próprio paciente sem receber seu identificador na URL.
        [HttpGet("me/historico-consultas")]
        [Authorize(Roles = PerfilDeUsuario.Paciente)]
        public async Task<ActionResult<PagedResultDTO<HistoricoConsultaDTO>>> ListarMeuHistoricoAsync(
            [FromQuery] HistoricoConsultaFiltroDTO filtro)
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual?.PerfilId is not Guid pacienteId)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            try
            {
                var historico = await _pacienteService.ListarHistoricoConsultasAsync(
                    pacienteId,
                    usuarioAtual.UsuarioId,
                    usuarioAtual.TipoUsuario,
                    filtro);
                return Ok(historico);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
        }
    }
}
