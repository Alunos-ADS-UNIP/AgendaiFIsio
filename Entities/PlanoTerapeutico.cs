using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class PlanoTerapeutico
    {
        public string Objetivo { get; set; }
        public int QtdSessoes { get; set; }
        public string Procedimentos { get; set; }
    }
}