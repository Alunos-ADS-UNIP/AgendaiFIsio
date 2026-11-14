using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Usuario
{
    // Define os dados necessários para criar uma conta.
    public class UsuarioRegisterDTO
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de usuário é obrigatório (ex: Paciente, Profissional).")]
        [AgendaiFisio.Validations.TipoUsuarioValido]
        public string TipoUsuario { get; set; } = string.Empty;

        [AgendaiFisio.Validations.AceiteLgpdObrigatorio]
        public bool AceiteLgpd { get; set; }

        // Obrigatória para pacientes, pois a API precisa aplicar a regra etária
        // antes de criar a conta. Profissionais informam a data ao completar o perfil.
        public DateTime? DataNascimento { get; set; }

        public bool ConsentimentoResponsavelLegal { get; set; }

        [MaxLength(150, ErrorMessage = "O nome do responsável deve ter no máximo 150 caracteres.")]
        public string? NomeResponsavelLegal { get; set; }
    }
}
