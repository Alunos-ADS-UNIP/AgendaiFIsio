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
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        public string Cpf { get; set; } = string.Empty; 

        // Adicione a data de nascimento aqui:
        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        public DateTime DataNascimento { get; set; }

        public string Telefone { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;
        
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
    }
}