using System;

namespace AgendaiFisio.DTOs.Especialidade
{
    // Dados da especialidade enviados pela API.
    public class EspecialidadeResponseDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
