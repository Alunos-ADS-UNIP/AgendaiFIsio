using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.DTOs.Profissional
{
    // Único campo aceito para escolher ou substituir a especialidade do profissional. Não existe
    // forma de "limpar" o vínculo por aqui: enviar null é rejeitado (ver ProfissionalController).
    public class ProfissionalEspecialidadeUpdateDTO
    {
        [Required(ErrorMessage = "A especialidade é obrigatória.")]
        public Guid? EspecialidadeId { get; set; }
    }
}
