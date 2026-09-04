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
using AgendaiFisio.Entities;

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

        public async Task<UsuarioResponseDTO> RegistrarAsync(UsuarioRegisterDTO registroDto)
        {
            // 1. Verifica se já existe um usuário com este e-mail
            var usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == registroDto.Email);
                
            if (usuarioExistente != null)
                throw new Exception("Já existe um usuário cadastrado com este e-mail.");

            // 2. Cria a entidade Usuario criptografando a senha com BCrypt
            var novoUsuario = new Usuario
            {
                Email = registroDto.Email.ToLower(),
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(registroDto.Senha),
                TipoUsuario = registroDto.TipoUsuario
            };

            _context.Usuarios.Add(novoUsuario);

            // 3. Criação automática do perfil correspondente
            if (registroDto.TipoUsuario.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
            {
                var novoPaciente = new Entities.Paciente
                {
                    UsuarioId = novoUsuario.Id,
                    NomeCompleto = "Cadastro Pendente",
                    Cpf = string.Empty,
                    Telefone = string.Empty,
                    Sexo = string.Empty,
                    EstadoCivil = string.Empty,
                    Endereco = new Entities.Endereco()
                    {
                        Rua = string.Empty,
                        Numero = string.Empty,
                        Complemento = string.Empty,
                        Cep = string.Empty,
                        Bairro = string.Empty,
                        Cidade = string.Empty,
                        Estado = string.Empty
                    }
                };
                _context.Pacientes.Add(novoPaciente);
            }
            else if (registroDto.TipoUsuario.Equals("Profissional", StringComparison.OrdinalIgnoreCase))
            {
                var novoProfissional = new Entities.Profissional
                {
                    UsuarioId = novoUsuario.Id,
                    NomeCompleto = "Cadastro Pendente",
                    Cpf = string.Empty,
                    Crefito = string.Empty,
                    Telefone = string.Empty,
                    Especialidade = string.Empty
                };
                _context.Profissionais.Add(novoProfissional);
            }

            // 4. Salva o Usuario e o Paciente/Profissional em uma única transação
            await _context.SaveChangesAsync();

            // 5. Retorna os dados mapeados
            return new UsuarioResponseDTO
            {
                Id = novoUsuario.Id, 
                Email = novoUsuario.Email,
                TipoUsuario = novoUsuario.TipoUsuario
            };
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

        private string GerarTokenJwt(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            
            
            var key = Encoding.ASCII.GetBytes(secretKey!); 

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.TipoUsuario) 
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