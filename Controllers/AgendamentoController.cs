using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.Services.Agendamento;

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
            var agendamentos = await _agendamentoService.ListAsync(
                filtros.Data,
                filtros.ProfissionalId,
                filtros.Status);

            return Ok(agendamentos.Select(ToResponse).ToList());
        }

        // Busca todos os detalhes de um agendamento específico.
        [HttpGet("{id:guid}", Name = "ObterAgendamentoPorId")]
        public async Task<ActionResult<AgendamentoResponseDTO>> GetByIdAsync(Guid id)
        {
            var agendamento = await _agendamentoService.GetByIdAsync(id);

            if (agendamento == null)
                return NotFound(new { erro = "Agendamento não encontrado." });

            return Ok(ToResponse(agendamento));
        }

        // Reserva um horário para um paciente com um profissional.
        [HttpPost]
        public async Task<ActionResult<AgendamentoResponseDTO>> CreateAsync(
            [FromBody] AgendamentoCreateDTO dto)
        {
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

                var criado = await _agendamentoService.CreateAsync(agendamento);

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
                DataHora = agendamento.DataHora,
                Status = agendamento.Status,
                Observacoes = agendamento.Observacoes
            };
        }
    }
}
