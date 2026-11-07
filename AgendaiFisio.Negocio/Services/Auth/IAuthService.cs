using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Services.Auth
{
    // Lista as ações disponíveis para autenticação.
    public interface IAuthService
    {
        // Confere os dados e realiza o login.
        Task<LoginResponseDTO> RealizarLoginAsync(UsuarioLoginDTO loginDTO);
        // Cria uma nova conta de usuário.
        Task<UsuarioResponseDTO> RegistrarAsync(UsuarioRegisterDTO registroDto);
    }
}
