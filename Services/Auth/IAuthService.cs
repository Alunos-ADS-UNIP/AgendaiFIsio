using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Usuario;

namespace AgendaiFisio.Services.Auth
{
    public interface IAuthService
    {
        Task<string> RealizarLoginAsync(UsuarioLoginDTO loginDTO);
        Task<UsuarioResponseDTO> RegistrarAsync(UsuarioRegisterDTO registroDto);
    }
}