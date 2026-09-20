using System;

namespace AgendaiFisio.DTOs.Profissional
{
    // Traz só os campos necessários para exibir a lista de fisioterapeutas.
    public class ProfissionalListItemDTO
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Especialidade { get; set; } = string.Empty;
        public string Crefito { get; set; } = string.Empty;
        public bool Ativo { get; set; }
    }
}