using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    // Guarda o plano de tratamento de uma avaliação.
    public class PlanoTerapeutico
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Objetivo { get; set; } = string.Empty;
        public int QtdSessoes { get; set; }
        public string Procedimentos { get; set; } = string.Empty;


        public Guid AvaliacaoFisioterapeutaId { get; set; }
        public virtual AvaliacaoFisioterapeuta? AvaliacaoFisioterapeuta { get; set; }
    }
}