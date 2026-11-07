using System;

namespace AgendaiFisio.DTOs.Profissional
{
    // Período em que o fisioterapeuta não pode ser agendado: um bloqueio dele ou uma consulta já
    // marcada. Só traz início e fim (UTC), sem motivo nem dados de paciente.
    public class IndisponibilidadeDTO
    {
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }
    }
}
