using System;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.DTOs.Profissional
{
    // Traz só os campos necessários para exibir a lista de fisioterapeutas.
    public class ProfissionalListItemDTO
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;

        // Null quando o profissional ainda não escolheu nenhuma especialidade do catálogo.
        public EspecialidadeResponseDTO? Especialidade { get; set; }

        public string Crefito { get; set; } = string.Empty;
        public bool Ativo { get; set; }
    }
}