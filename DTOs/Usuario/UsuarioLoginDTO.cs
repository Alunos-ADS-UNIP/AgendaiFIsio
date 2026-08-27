using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.DTOs.Usuario
{
    public class UsuarioLoginDTO
    {
        [Required(ErrorMessage = "O E-mail é obrigatório.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        public string Senha { get; set; } = string.Empty;
    }
}