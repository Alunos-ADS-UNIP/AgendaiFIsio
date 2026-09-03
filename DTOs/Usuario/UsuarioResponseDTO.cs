using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.DTOs.Usuario
{
    public class UsuarioResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string TipoUsuario { get; set; } // EX: "Admin", "Paciente", "Profissional"
    }
}