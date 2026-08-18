using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class AvaliacaoFisioterapeuta
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string QueixaPrincipal { get; set; }
        public string HistoriaPregresssa { get; set; }
        public string HabitosDeVida { get; set; }
        public string TratamentosRealizados { get; set; }
        public string AntecedentesPessoaisFamiliares { get; set; }
        public DateTime DataAvaliacao { get; set; }
        public PlanoTerapeutico Plano { get; set; }
        public Profissional Profissional { get; set; }
    }
}