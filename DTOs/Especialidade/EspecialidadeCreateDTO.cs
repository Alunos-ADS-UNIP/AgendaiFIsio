using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Especialidade
{
    // Dados necessários para cadastrar uma nova especialidade.
    public class EspecialidadeCreateDTO
    {
        [Required(ErrorMessage = "O nome da especialidade é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }
}
