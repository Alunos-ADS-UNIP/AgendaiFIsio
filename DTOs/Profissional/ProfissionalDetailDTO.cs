using System;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.DTOs.Profissional
{
    // Traz todos os campos relevantes de um fisioterapeuta específico.
    public class ProfissionalDetailDTO
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Crefito { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }

        // Null quando o profissional ainda não escolheu nenhuma especialidade do catálogo.
        public EspecialidadeResponseDTO? Especialidade { get; set; }

        public string Bio { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
