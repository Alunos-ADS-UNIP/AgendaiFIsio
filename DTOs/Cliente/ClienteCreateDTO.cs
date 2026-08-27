using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Cliente
{
    public class ClienteCreateDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [ProibirValores("string", "teste", "admin", ErrorMessage = "O nome informado é inválido ou um valor padrão do sistema.")]
        public required string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress]
        [ProibirValores("user@example.com", "string", ErrorMessage = "Por favor, insira um e-mail real.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [CpfValido(ErrorMessage = "O CPF informado é matematicamente inválido.")] 
        public required string Cpf { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [RegularExpression(@"^\(?\d{2}\)?[\s-]?\d{4,5}-?\d{4}$", ErrorMessage = "Formato de telefone inválido.")]
        public string? Telefone { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        public required string Senha { get; set; }
    }
}