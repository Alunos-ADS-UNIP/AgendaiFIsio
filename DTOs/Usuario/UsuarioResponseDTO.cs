using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.DTOs.Usuario
{
    // Define os dados públicos devolvidos após o cadastro.
    public class UsuarioResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
    }
}