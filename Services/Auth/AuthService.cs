using System;
using System.IdentityModel.Tokens.Jwt;
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
            // Confere e normaliza o tipo de usuário. "Clinica" (admin) nunca passa por aqui — só é
            // provisionada por um mecanismo confiável (ver Program.cs). O [TipoUsuarioValido] no
            // DTO já bloqueia isso no model binding; esta linha é a segunda camada, para quando
            // o serviço é chamado direto sem passar por ele.
            var tipoUsuario = AgendaiFisio.Constants.PerfilDeUsuario.NormalizarCadastro(registroDto.TipoUsuario)
                ?? throw new ArgumentException("Tipo de usuario invalido. Use Paciente ou Profissional.");

            // Procura uma conta já cadastrada com o mesmo e-mail.
            var usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == registroDto.Email);

            if (usuarioExistente != null)
                throw new Exception("Já existe um usuário cadastrado com este e-mail.");

            // Guarda a senha protegida, e não o texto original.
            var novoUsuario = new Usuario
            {
                Email = registroDto.Email.ToLowerInvariant(),
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(registroDto.Senha),
                TipoUsuario = tipoUsuario
            };

            _context.Usuarios.Add(novoUsuario);

            // Cria um perfil vazio conforme o tipo de usuário.
            if (tipoUsuario.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
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
            else if (tipoUsuario.Equals("Profissional", StringComparison.OrdinalIgnoreCase))
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
        public async Task<LoginResponseDTO> RealizarLoginAsync(UsuarioLoginDTO loginDTO)
        {
            // Procura o usuário pelo e-mail informado.
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Paciente)
                .Include(u => u.Profissional)
                .FirstOrDefaultAsync(u => u.Email == loginDTO.Email.Trim().ToLowerInvariant());

            if (usuario == null)
            {
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
            }

            bool senhaValida = BCrypt.Net.BCrypt.Verify(loginDTO.Senha, usuario.SenhaHash);

            if (!senhaValida)
            {
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
            }

            var tipoUsuario = PerfilDeUsuario.NormalizarAutenticado(usuario.TipoUsuario)
                ?? throw new InvalidOperationException("O tipo de usuário cadastrado é inválido.");

            var perfilId = tipoUsuario switch
            {
                PerfilDeUsuario.Paciente => usuario.Paciente?.Id,
                PerfilDeUsuario.Profissional => usuario.Profissional?.Id,
                _ => null
            };

            if (tipoUsuario != PerfilDeUsuario.Admin && perfilId is null)
                throw new InvalidOperationException("O perfil do usuário não foi encontrado.");

            // Gera o token depois de validar a conta e localizar seu perfil de domínio.
            return GerarTokenJwt(usuario, tipoUsuario, perfilId);
        }

        // Monta o token com os dados e o tempo de validade do usuário.
        private LoginResponseDTO GerarTokenJwt(
            Usuario usuario,
            string tipoUsuario,
            Guid? perfilId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            var key = Encoding.ASCII.GetBytes(secretKey!);
            var expiraEmUtc = DateTime.UtcNow.AddHours(
                jwtSettings.GetValue<double>("ExpirationHours"));

            var claims = new List<Claim>
            {
                new(JwtClaimNames.JwtId, Guid.NewGuid().ToString()),
                new(JwtClaimNames.Subject, usuario.Id.ToString()),
                new(JwtClaimNames.Email, usuario.Email),
                new(JwtClaimNames.Role, tipoUsuario)
            };

            if (perfilId.HasValue)
                claims.Add(new Claim(JwtClaimNames.PerfilId, perfilId.Value.ToString()));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiraEmUtc,
                Issuer = jwtSettings.GetValue<string>("Issuer"),
                Audience = jwtSettings.GetValue<string>("Audience"),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            // Cria o token e transforma-o em texto para a resposta.
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new LoginResponseDTO
            {
                AccessToken = tokenHandler.WriteToken(token),
                ExpiresAtUtc = expiraEmUtc,
                Usuario = new UsuarioAutenticadoDTO
                {
                    UsuarioId = usuario.Id,
                    PerfilId = perfilId,
                    Email = usuario.Email,
                    TipoUsuario = tipoUsuario
                }
            };
        }
    }
}
