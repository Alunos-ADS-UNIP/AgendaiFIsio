using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AgendaiFisio.Constants;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.Services.Especialidade;

namespace AgendaiFisio.Controllers
{
    // Recebe solicitações de listagem e cadastro das especialidades.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EspecialidadeController : ControllerBase
    {
        private readonly IEspecialidadeService _especialidadeService;
        private readonly ILogger<EspecialidadeController> _logger;

        public EspecialidadeController(IEspecialidadeService especialidadeService, ILogger<EspecialidadeController> logger)
        {
            _especialidadeService = especialidadeService;
            _logger = logger;
        }

        // Lista todas as especialidades, em ordem alfabética. Fica acessível sem login porque a
        // tela de cadastro do profissional precisa dela antes de haver qualquer token.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar()
        {
            try
            {
                var especialidades = await _especialidadeService.ListarAsync();
                return Ok(especialidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao listar especialidades.");
                return StatusCode(500, new { erro = "Erro interno ao processar a solicitação." });
            }
        }

        // Devolve uma especialidade específica. Mesma visibilidade da listagem (sem login).
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            try
            {
                var especialidade = await _especialidadeService.ObterPorIdAsync(id);

                if (especialidade is null)
                {
                    return NotFound(new { erro = "Especialidade não encontrada." });
                }

                return Ok(especialidade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao buscar especialidade {Id}.", id);
                return StatusCode(500, new { erro = "Erro interno ao processar a solicitação." });
            }
        }

        // Cadastra uma nova especialidade, que já fica disponível para os fisioterapeutas.
        // Só a Clínica cadastra; nenhum profissional cria especialidades pela API.
        [HttpPost]
        [Authorize(Roles = PerfilDeUsuario.Admin)]
        public async Task<IActionResult> Criar([FromBody] EspecialidadeCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var criada = await _especialidadeService.CriarAsync(dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
            }
            catch (ArgumentException ex)
            {
                // Nome inválido depois de normalizado (ex.: só espaços, ou menos de 3 letras).
                return BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Informa quando já existe uma especialidade com o mesmo nome.
                return Conflict(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao cadastrar especialidade.");
                return StatusCode(500, new { erro = "Erro interno ao processar a solicitação." });
            }
        }
    }
}
