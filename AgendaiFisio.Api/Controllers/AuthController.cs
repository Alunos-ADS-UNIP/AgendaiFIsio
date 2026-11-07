using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Controllers
{
    // Recebe as solicitações de cadastro e login.
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUsuarioAtualService _usuarioAtualService;

        // Guarda o serviço que executa as regras de autenticação.
        public AuthController(
            IAuthService authService,
            IUsuarioAtualService usuarioAtualService)
        {
            _authService = authService;
            _usuarioAtualService = usuarioAtualService;
        }

        // Cria uma nova conta de usuário.
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegisterDTO registroDto)
        {
            try
            {
                var usuario = await _authService.RegistrarAsync(registroDto);
                return Ok(new { mensagem = "Usuário cadastrado com sucesso!", dados = usuario });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Confere os dados e devolve um token de acesso.
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] UsuarioLoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                // Informa quando os dados enviados estão incompletos.
                return BadRequest(ModelState);
            }

            try
            {
                var resultado = await _authService.RealizarLoginAsync(loginDTO);
                return Ok(resultado);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Informa quando o login não é autorizado.
                return Unauthorized(new { Erro = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Erro = "Ocorreu um erro interno no servidor.", Detalhe = ex.Message });
            }
        }

        // Devolve a identidade assinada no JWT da requisição atual.
        [Authorize]
        [HttpGet("me")]
        public ActionResult<UsuarioAutenticadoDTO> Me()
        {
            var usuarioAtual = _usuarioAtualService.Obter();
            if (usuarioAtual is null)
                return Unauthorized(new { erro = "Usuário não identificado no token." });

            return Ok(new UsuarioAutenticadoDTO
            {
                UsuarioId = usuarioAtual.UsuarioId,
                PerfilId = usuarioAtual.PerfilId,
                Email = usuarioAtual.Email,
                TipoUsuario = usuarioAtual.TipoUsuario
            });
        }

        // O JWT é stateless; esta rota encerra a sessão no cliente, que descarta o token.
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout() => NoContent();

    }
}
