using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Services.Auth
{
    public class AuthService
    {
        private readonly DistribuidoraContext _context;

        public AuthService(DistribuidoraContext context)
        {
            _context = context;
        }
        public string RealizarLogin(UsuarioLoginDTO loginDTO)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == loginDTO.Email && u.SenhaHash == loginDTO.Senha);

            if (usuario == null)
            {
                throw new UnauthorizedAccessException("Credenciais inválidas.");
            }
            bool senhaValida = BCrypt.Net.BCrypt.Verify(loginDTO.Senha, usuario.SenhaHash);

            if (!senhaValida)
            {
                throw new UnauthorizedAccessException("Credenciais inválidas.");
            }

            return $"Login realizado com sucesso para o usuário: {usuario.Email}";
        }
    }
}