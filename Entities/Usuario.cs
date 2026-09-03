using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; }
        public string SenhaHash { get; set; }

        public string TipoUsuario { get; set; } // EX: "Clinica", "Paciente", "Profissional"

        public virtual Paciente Paciente { get; set; } // Propriedade de navegação para o Paciente associado    

        public virtual Profissional Profissional { get; set; } // Propriedade de navegação para o Profissional associado
    }
}