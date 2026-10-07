using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.Services.Agendamento;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Constants;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações relacionadas à agenda dos profissionais.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgendamentoController : ControllerBase
    {
        private readonly IAgendamentoService _agendamentoService;
        private readonly IUsuarioAtualService _usuarioAtualService;

        public AgendamentoController(
            IAgendamentoService agendamentoService,
            IUsuarioAtualService usuarioAtualService)
        {
            _agendamentoService = agendamentoService;
            _usuarioAtualService = usuarioAtualService;
        }

        // Lista agendamentos e aplica os filtros informados na consulta.
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AgendamentoResponseDTO>>> ListAsync(
            [FromQuery] AgendamentoFilterDTO filtros)
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual is null)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var agendamentos = await _agendamentoService.ListAsync(
                usuarioAtual.UsuarioId,
                usuarioAtual.TipoUsuario,
                filtros.Data,
                filtros.ProfissionalId,
                filtros.Status);

            return Ok(agendamentos.Select(ToResponse).ToList());
        }

        // Busca todos os detalhes de um agendamento específico.
        [HttpGet("{id:guid}", Name = "ObterAgendamentoPorId")]
        public async Task<ActionResult<AgendamentoResponseDTO>> GetByIdAsync(Guid id)
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual is null)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            var agendamento = await _agendamentoService.GetByIdAsync(
                id,
                usuarioAtual.UsuarioId,
                usuarioAtual.TipoUsuario);

            if (agendamento == null)
                return NotFound(new { erro = "Agendamento não encontrado." });

            return Ok(ToResponse(agendamento));
        }

        // Reserva um horário para o próprio paciente autenticado.
        [HttpPost]
        [Authorize(Roles = PerfilDeUsuario.Paciente)]
        public async Task<ActionResult<AgendamentoResponseDTO>> CreateAsync(
            [FromBody] AgendamentoCreateDTO dto)
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual?.PerfilId is not Guid pacienteId)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            try
            {
                var agendamento = new Entities.Agendamento
                {
                    PacienteId = pacienteId,
                    ProfissionalId = dto.ProfissionalId,
                    DataHora = dto.DataHora,
                    Status = dto.Status?.Trim() ?? string.Empty,
                    Observacoes = dto.Observacoes
                };

                var criado = await _agendamentoService.CreateForPatientAsync(agendamento, pacienteId);

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
        }

        // Permite à Clínica agendar em nome de um paciente explicitamente informado.
        [HttpPost("administrativo")]
        [Authorize(Roles = PerfilDeUsuario.Admin)]
        public async Task<ActionResult<AgendamentoResponseDTO>> CreateForAdminAsync(
            [FromBody] AgendamentoAdminCreateDTO dto)
        {
            if (_usuarioAtualService.Obter() is null)
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

                var criado = await _agendamentoService.CreateForAdminAsync(agendamento);

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
