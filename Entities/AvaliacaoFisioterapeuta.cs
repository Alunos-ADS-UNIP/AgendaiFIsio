using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class AvaliacaoFisioterapeuta
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string QueixaPrincipal { get; set; } = string.Empty;
        public string HistoriaPregressa { get; set; } = string.Empty;
        public string HabitosDeVida { get; set; } = string.Empty;
        public string TratamentosRealizados { get; set; } = string.Empty;
        public string AntecedentesPessoaisFamiliares { get; set; } = string.Empty;
        public DateTime DataAvaliacao { get; set; } = DateTime.UtcNow;

        
        public Guid ProfissionalId { get; set; }
        public virtual Profissional? Profissional { get; set; }

        
        public Guid PacienteId { get; set; }
        public virtual Paciente? Paciente { get; set; }

        
        public virtual PlanoTerapeutico? Plano { get; set; }
    }
}