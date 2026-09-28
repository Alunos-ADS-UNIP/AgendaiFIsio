using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public EspecialidadeController(IEspecialidadeService especialidadeService)
        {
            _especialidadeService = especialidadeService;
        }

        // Lista todas as especialidades, para uso em filtros e no cadastro dos fisioterapeutas.
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
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Cadastra uma nova especialidade, que já fica disponível para os fisioterapeutas.
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
                return Created($"api/especialidade/{criada.Id}", criada);
            }
            catch (InvalidOperationException ex)
            {
                // Informa quando já existe uma especialidade com o mesmo nome.
                return Conflict(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }
    }
}
