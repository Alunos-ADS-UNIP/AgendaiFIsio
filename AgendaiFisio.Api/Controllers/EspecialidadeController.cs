using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AgendaiFisio.Services.Especialidade;

namespace AgendaiFisio.Controllers
{
    // Expõe o catálogo fixo de especialidades usado no MVP.
    [ApiController]
    [Route("api/[controller]")]
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

    }
}
