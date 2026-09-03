using System;

namespace AgendaiFisio.DTOs.Profissional
{
    public class ProfissionalResponseDTO
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Crefito { get; set; } = string.Empty;
        public string Especialidade { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        
        // Retorna o status de aprovação (true = aprovado, false = pendente de análise)
        public bool Ativo { get; set; } 
    }
}