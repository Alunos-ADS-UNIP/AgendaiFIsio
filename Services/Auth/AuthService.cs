using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AgendaiFisio.Constants;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Usuario;
using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.Auth
{
    // Aplica as regras de cadastro, login e criação de tokens.
    public class AuthService : IAuthService 
    {
        // Papéis que uma conta pode escolher no autocadastro público. "Clinica" (admin) fica de
        // fora de propósito: só é provisionado por um mecanismo confiável (ver Program.cs).
        private static readonly string[] TiposPermitidosNoAutocadastro =
        {
            PerfilDeUsuario.Paciente,
            PerfilDeUsuario.Profissional
        };

        private readonly AgendaiFisioDbContext _context;
        private readonly IConfiguration _configuration;

        // Recebe o banco e as configurações da aplicação.
        public AuthService(AgendaiFisioDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Cadastra o usuário e cria seu perfil inicial.
        public async Task<UsuarioResponseDTO> RegistrarAsync(UsuarioRegisterDTO registroDto)
        {
            // Impede que o cadastro público conceda o papel de Clínica (admin) ou qualquer outro
            // valor fora da lista — sem isso, bastava registrar com TipoUsuario="Clinica" para
            // ganhar acesso de administrador (ver docs/especialidades.md, achado 2 da revisão).
            var tipoPermitido = TiposPermitidosNoAutocadastro
                .Any(t => t.Equals(registroDto.TipoUsuario, StringComparison.OrdinalIgnoreCase));

            if (!tipoPermitido)
            {
                throw new InvalidOperationException("Tipo de usuário inválido para autocadastro.");
            }

            // Procura uma conta já cadastrada com o mesmo e-mail.
            var usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == registroDto.Email);

            if (usuarioExistente != null)
                throw new Exception("Já existe um usuário cadastrado com este e-mail.");

            // Guarda a senha protegida, e não o texto original.
            var novoUsuario = new Usuario
            {
                Email = registroDto.Email.ToLower(),
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(registroDto.Senha),
                TipoUsuario = registroDto.TipoUsuario
            };

            _context.Usuarios.Add(novoUsuario);

            // Cria um perfil vazio conforme o tipo de usuário.
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
                    Telefone = string.Empty
                };
                _context.Profissionais.Add(novoProfissional);
            }

            // Salva a conta e o perfil no banco.
            await _context.SaveChangesAsync();

            // Devolve apenas os dados públicos do usuário.
            return new UsuarioResponseDTO
            {
                Id = novoUsuario.Id, 
                Email = novoUsuario.Email,
                TipoUsuario = novoUsuario.TipoUsuario
            };
        }

        // Confere o login e cria um token para o usuário.
        public async Task<string> RealizarLoginAsync(UsuarioLoginDTO loginDTO)
        {
            // Procura o usuário pelo e-mail informado.
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

            // Gera o token depois de validar os dados.
            return GerarTokenJwt(usuario);
        }

        // Monta o token com os dados e o tempo de validade do usuário.
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

            // Cria o token e transforma-o em texto para a resposta.
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}