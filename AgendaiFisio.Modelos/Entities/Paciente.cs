using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    // Guarda os dados pessoais e o endereço do paciente.
    public class Paciente
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeCompleto { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;


        public virtual Endereco Endereco { get; set; } = null!; // Relação obrigatória carregada pelo EF.

        public Guid UsuarioId { get; set; }

        public virtual Usuario Usuario { get; set; } = null!; // Carregado pelo EF quando incluído na consulta.
    }
}
