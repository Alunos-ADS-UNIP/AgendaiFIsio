using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    // Guarda os dados do fisioterapeuta cadastrado.
    public class Profissional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeCompleto { get; set; }
        public string Cpf { get; set; }
        public string Crefito { get; set; }
        public string Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public string Especialidade { get; set; }
        public string Bio { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; } = true;

        public Guid UsuarioId { get; set; } 
        public virtual Usuario Usuario { get; set; } 
    }
}