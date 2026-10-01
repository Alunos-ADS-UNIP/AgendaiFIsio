using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Validations;

namespace AgendaiFisio.DTOs.Paciente
{
    // Define os dados usados para atualizar um paciente.
    public class PacienteUpdateDTO
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [AgendaiFisio.Validations.CpfValido(ErrorMessage = "O CPF informado é inválido.")]
        public string Cpf { get; set; } = string.Empty; 

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        public DateTime DataNascimento { get; set; }

        public string Telefone { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;
        
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        // Campos omitidos preservam o endereço atual; texto vazio limpa o campo.
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string Cep { get; set; } = string.Empty;
    }
}