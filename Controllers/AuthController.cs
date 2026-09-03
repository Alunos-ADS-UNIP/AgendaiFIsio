using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // A rota ficará: api/auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")] // A rota completa ficará: POST api/auth/login
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDTO loginDTO)
        {
            // O [ApiController] já valida o ModelState (Required, EmailAddress) automaticamente,
            // mas é sempre bom manter o código limpo.
            
            try
            {
                // Chama o serviço que busca o usuário e gera o JWT
                string token = await _authService.RealizarLoginAsync(loginDTO);

                // Retorna HTTP 200 (OK) com o token no formato JSON
                return Ok(new 
                { 
                    Message = "Login realizado com sucesso.",
                    Token = token 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Retorna HTTP 401 (Unauthorized) se a senha ou email estiverem errados
                return Unauthorized(new { Erro = ex.Message });
            }
            catch (Exception ex)
            {
                // Retorna HTTP 500 para qualquer outro erro inesperado
                return StatusCode(500, new { Erro = "Ocorreu um erro interno no servidor.", Detalhe = ex.Message });
            }
        }
    }
}