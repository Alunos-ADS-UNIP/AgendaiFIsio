using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Profissional
{
    public class ProfissionalUpdateDTO
    {
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O Telefone é obrigatório.")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Especialidade é obrigatória.")]
        public string Especialidade { get; set; } = string.Empty;
    }
}