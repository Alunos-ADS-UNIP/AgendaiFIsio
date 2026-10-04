using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Especialidade
{
    // Dados necessários para cadastrar uma nova especialidade.
    public class EspecialidadeCreateDTO
    {
        // O mínimo de 3 caracteres só é conferido depois de normalizar o nome (aparar espaços e
        // colapsar os internos) — ver EspecialidadeService.CriarAsync. Checar antes do Trim
        // deixaria passar algo como " a " (3 caracteres brutos, 1 depois de normalizado).
        [Required(ErrorMessage = "O nome da especialidade é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }
}
