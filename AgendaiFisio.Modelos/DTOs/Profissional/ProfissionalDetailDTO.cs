using System;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.DTOs.Profissional
{
    // Traz todos os campos relevantes de um fisioterapeuta específico.
    public class ProfissionalDetailDTO
    {
        public Guid Id { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Crefito { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;

        // Null quando o profissional ainda não escolheu nenhuma especialidade do catálogo.
        public EspecialidadeResponseDTO? Especialidade { get; set; }

        public string Bio { get; set; } = string.Empty;
        public string? FotoPerfilUrl { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public string Email { get; set; } = string.Empty;
        public EnderecoProfissionalDTO? Endereco { get; set; }
    }

    public class EnderecoProfissionalDTO
    {
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
