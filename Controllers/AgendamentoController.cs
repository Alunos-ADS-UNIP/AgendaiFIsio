using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.Services.Agendamento;
using AgendaiFisio.Constants;
using System.Security.Claims;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações relacionadas à agenda dos profissionais.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgendamentoController : ControllerBase
    {
        private readonly IAgendamentoService _agendamentoService;

        public AgendamentoController(IAgendamentoService agendamentoService)
        {
            _agendamentoService = agendamentoService;
        }

        // Lista agendamentos e aplica os filtros informados na consulta.
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AgendamentoResponseDTO>>> ListAsync(
            [FromQuery] AgendamentoFilterDTO filtros)
        {
            if (!TryGetUsuario(out var usuarioId, out var tipoUsuario))
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var agendamentos = await _agendamentoService.ListAsync(
                usuarioId,
                tipoUsuario,
                filtros.Data,
                filtros.ProfissionalId,
                filtros.Status);

            return Ok(agendamentos.Select(ToResponse).ToList());
        }

        // Busca todos os detalhes de um agendamento específico.
        [HttpGet("{id:guid}", Name = "ObterAgendamentoPorId")]
        public async Task<ActionResult<AgendamentoResponseDTO>> GetByIdAsync(Guid id)
        {
            if (!TryGetUsuario(out var usuarioId, out var tipoUsuario))
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var agendamento = await _agendamentoService.GetByIdAsync(id, usuarioId, tipoUsuario);

            if (agendamento == null)
                return NotFound(new { erro = "Agendamento não encontrado." });

            return Ok(ToResponse(agendamento));
        }

        // Reserva um horário para um paciente com um profissional.
        [HttpPost]
        [Authorize(Roles = PerfilDeUsuario.Paciente + "," + PerfilDeUsuario.Admin)]
        public async Task<ActionResult<AgendamentoResponseDTO>> CreateAsync(
            [FromBody] AgendamentoCreateDTO dto)
        {
            if (!TryGetUsuario(out var usuarioId, out var tipoUsuario))
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            try
            {
                var agendamento = new Entities.Agendamento
                {
                    PacienteId = dto.PacienteId,
                    ProfissionalId = dto.ProfissionalId,
                    DataHora = dto.DataHora,
                    Status = dto.Status?.Trim() ?? string.Empty,
                    Observacoes = dto.Observacoes
                };

                var criado = await _agendamentoService.CreateAsync(agendamento, usuarioId, tipoUsuario);

                return CreatedAtRoute(
                    "ObterAgendamentoPorId",
                    new { id = criado.Id },
                    ToResponse(criado));
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        private bool TryGetUsuario(out Guid usuarioId, out string tipoUsuario)
        {
            var idValido = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out usuarioId);
            tipoUsuario = User.IsInRole(PerfilDeUsuario.Paciente) ? PerfilDeUsuario.Paciente
                : User.IsInRole(PerfilDeUsuario.Profissional) ? PerfilDeUsuario.Profissional
                : User.IsInRole(PerfilDeUsuario.Admin) ? PerfilDeUsuario.Admin
                : string.Empty;
            return idValido && tipoUsuario.Length > 0;
        }

        private static AgendamentoResponseDTO ToResponse(Entities.Agendamento agendamento)
        {
            return new AgendamentoResponseDTO
            {
                Id = agendamento.Id,
                PacienteId = agendamento.PacienteId,
                PacienteNome = agendamento.Paciente?.NomeCompleto ?? string.Empty,
                ProfissionalId = agendamento.ProfissionalId,
                ProfissionalNome = agendamento.Profissional?.NomeCompleto ?? string.Empty,
                DataHora = DateTime.SpecifyKind(agendamento.DataHora, DateTimeKind.Utc),
                Status = agendamento.Status,
                Observacoes = agendamento.Observacoes
            };
        }
    }
}
