using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Paciente
{
    public class PacienteUpdateDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [ProibirValores("string", "teste", "admin", ErrorMessage = "O nome informado é inválido ou um valor padrão do sistema.")]
        public string? Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress]
        [ProibirValores("user@example.com", "string", ErrorMessage = "Por favor, insira um e-mail real.")]
        public string? Email { get; set; }
        public string? Telefone { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        public string? Senha { get; set; }
    }
}