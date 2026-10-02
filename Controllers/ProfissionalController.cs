using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Services.Profissional;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações relacionadas à listagem, ao detalhe e à atualização do fisioterapeuta.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfissionalController : ControllerBase
    {
        private readonly IProfissionalService _profissionalService;
        private readonly ILogger<ProfissionalController> _logger;

        // Guarda o serviço usado para consultar e alterar o fisioterapeuta.
        public ProfissionalController(IProfissionalService profissionalService, ILogger<ProfissionalController> logger)
        {
            _profissionalService = profissionalService;
            _logger = logger;
        }

        // Lista os fisioterapeutas cadastrados, com filtros opcionais de nome, especialidade e status ativo.
        // Ex.: GET api/profissional?nome=ana&especialidadeId=00000000-0000-0000-0000-000000000015&ativo=true&pagina=1&tamanhoPagina=10
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] ProfissionalFiltroDTO filtro)
        {
            try
            {
                var resultado = await _profissionalService.ListarAsync(filtro);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Retorna os dados completos de um fisioterapeuta específico.
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            try
            {
                var profissional = await _profissionalService.ObterPorIdAsync(id);
                return Ok(profissional);
            }
            catch (KeyNotFoundException ex)
            {
                // Informa quando o fisioterapeuta não é localizado.
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Atualiza os dados do fisioterapeuta que está logado.
        [HttpPut("completar-perfil")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> AtualizarAsync([FromBody] ProfissionalUpdateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                // Informa quais campos obrigatórios não foram preenchidos corretamente.
                return BadRequest(ModelState);
            }

            try
            {
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioIdClaim))
                {
                    // Impede a atualização sem identificar o usuário.
                    return Unauthorized("Usuário não identificado no token.");
                }

                var usuarioId = Guid.Parse(usuarioIdClaim);

                await _profissionalService.AtualizarAsync(usuarioId, dto);

                return Ok(new { mensagem = "Perfil atualizado com sucesso!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Informa quando o CREFITO informado já pertence a outro fisioterapeuta.
                return Conflict(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                // Devolve ao cliente o erro ocorrido no processamento.
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Escolhe ou substitui a especialidade do fisioterapeuta logado. Fica fora do
        // completar-perfil de propósito: assim "não enviar o campo" sempre preserva o vínculo
        // atual, e só chamar esta rota (com um id válido) pode mudá-lo.
        [HttpPut("especialidade")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> AtualizarEspecialidadeAsync([FromBody] ProfissionalEspecialidadeUpdateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                // Cobre id ausente, nulo ou malformado (o model binding já rejeita um GUID inválido
                // ou uma lista no lugar de um único id antes de chegar aqui).
                return BadRequest(ModelState);
            }

            try
            {
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioIdClaim))
                {
                    return Unauthorized("Usuário não identificado no token.");
                }

                var usuarioId = Guid.Parse(usuarioIdClaim);

                await _profissionalService.AtualizarEspecialidadeAsync(usuarioId, dto.EspecialidadeId!.Value);

                return Ok(new { mensagem = "Especialidade atualizada com sucesso!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // Id vazio (Guid.Empty) ou que não existe no catálogo.
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar a especialidade do profissional.");
                return StatusCode(500, new { erro = "Erro interno ao processar a solicitação." });
            }
        }
    }
}