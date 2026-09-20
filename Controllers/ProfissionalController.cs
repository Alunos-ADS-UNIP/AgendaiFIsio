using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // Guarda o serviço usado para consultar e alterar o fisioterapeuta.
        public ProfissionalController(IProfissionalService profissionalService)
        {
            _profissionalService = profissionalService;
        }

        // Lista os fisioterapeutas cadastrados, com filtros opcionais de nome, especialidade e status ativo.
        // Ex.: GET api/profissional?nome=ana&especialidade=ortopedia&ativo=true&pagina=1&tamanhoPagina=10
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
    }
}