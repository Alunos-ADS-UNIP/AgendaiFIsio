using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Profissional
{
    public class ProfissionalCreateDTO
    {
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "O CPF deve ter 11 caracteres.")]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CREFITO é obrigatório.")]
        public string Crefito { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Telefone é obrigatório.")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Especialidade é obrigatória.")]
        public string Especialidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Data de Nascimento é obrigatória.")]
        public DateTime DataNascimento { get; set; }
    }
}