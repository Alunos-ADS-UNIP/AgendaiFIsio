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

        // Registra o aceite informado na criação da conta. A data permite
        // comprovar quando o consentimento foi fornecido.
        public bool AceiteLgpd { get; set; }
        public DateTime? AceiteLgpdEm { get; set; }

        public virtual Paciente? Paciente { get; set; }

        public virtual Profissional? Profissional { get; set; }
    }
}
