using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Profissional
{
    // Campos que o fisioterapeuta pode preencher ou corrigir no próprio perfil.
    public class ProfissionalUpdateDTO
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 150 caracteres.")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "O CPF deve conter 11 dígitos numéricos, sem pontos ou traços.")]
        [AgendaiFisio.Validations.CpfValido(ErrorMessage = "O CPF informado é inválido.")]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CREFITO é obrigatório.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "O CREFITO deve ter entre 4 e 20 caracteres.")]
        public string Crefito { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "O telefone deve conter 10 ou 11 dígitos numéricos, com DDD.")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        public DateTime DataNascimento { get; set; }

        // A especialidade não faz parte deste contrato: ela é escolhida/trocada só pelo
        // PUT api/profissional/especialidade (ver ProfissionalEspecialidadeUpdateDTO). Omitir
        // este campo aqui nunca apaga nem altera o vínculo já salvo.

        [StringLength(1000, ErrorMessage = "A bio pode ter no máximo 1000 caracteres.")]
        public string Bio { get; set; } = string.Empty;

        [Required(ErrorMessage = "A rua do local de atendimento é obrigatória.")]
        [StringLength(150, ErrorMessage = "A rua pode ter no máximo 150 caracteres.")]
        public string Rua { get; set; } = string.Empty;

        [Required(ErrorMessage = "O número do local de atendimento é obrigatório.")]
        [StringLength(20, ErrorMessage = "O número pode ter no máximo 20 caracteres.")]
        public string Numero { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "O complemento pode ter no máximo 100 caracteres.")]
        public string Complemento { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CEP do local de atendimento é obrigatório.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "O CEP deve conter 8 dígitos numéricos.")]
        public string Cep { get; set; } = string.Empty;

        [Required(ErrorMessage = "O bairro do local de atendimento é obrigatório.")]
        [StringLength(100, ErrorMessage = "O bairro pode ter no máximo 100 caracteres.")]
        public string Bairro { get; set; } = string.Empty;

        [Required(ErrorMessage = "A cidade do local de atendimento é obrigatória.")]
        [StringLength(100, ErrorMessage = "A cidade pode ter no máximo 100 caracteres.")]
        public string Cidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O estado do local de atendimento é obrigatório.")]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "Informe a sigla do estado com 2 letras.")]
        public string Estado { get; set; } = string.Empty;

        public bool Ativo { get; set; }
    }
}
