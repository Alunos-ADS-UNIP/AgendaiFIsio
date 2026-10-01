using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    // Guarda os dados usados para acessar o sistema.
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;

        public string TipoUsuario { get; set; } = string.Empty;

        public virtual Paciente? Paciente { get; set; }

        public virtual Profissional? Profissional { get; set; }
    }
}
