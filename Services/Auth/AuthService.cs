using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Services.Auth
{
    public class AuthService : IAuthService 
    {
        private readonly AgendaiFisioDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AgendaiFisioDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> RealizarLoginAsync(UsuarioLoginDTO loginDTO)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == loginDTO.Email);

            if (usuario == null)
            {
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
            }

            bool senhaValida = BCrypt.Net.BCrypt.Verify(loginDTO.Senha, usuario.SenhaHash);

            if (!senhaValida)
            {
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
            }

            // Geração do Token JWT
            return GerarTokenJwt(usuario);
        }

        private string GerarTokenJwt(Entities.Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.TipoUsuario) // Isso define o papel (Paciente, Profissional, etc)
                }),
                Expires = DateTime.UtcNow.AddHours(jwtSettings.GetValue<double>("ExpirationHours")),
                Issuer = jwtSettings.GetValue<string>("Issuer"),
                Audience = jwtSettings.GetValue<string>("Audience"),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}