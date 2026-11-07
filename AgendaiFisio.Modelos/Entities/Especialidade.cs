using System;

namespace AgendaiFisio.Entities
{
    // Guarda os tipos de atendimento que os fisioterapeutas podem oferecer.
    public class Especialidade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nome { get; set; } = string.Empty;
    }
}
