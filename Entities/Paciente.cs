using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class Paciente
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeCompleto { get; set; }
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public string Sexo { get; set; }
        public string EstadoCivil { get; set; } //Ex: "Solteiro(a)", "Casado(a)", "Separado(a)", "Divorciado(a)", "Viúvo(a)"
        
        
        public virtual Endereco Endereco { get; set; }

        public Guid UsuarioId { get; set; } 

        public virtual Usuario Usuario { get; set; } 
    }
}