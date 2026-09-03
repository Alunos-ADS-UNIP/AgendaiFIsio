using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Entities
{
    public class Profissional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NomeCompleto { get; set; }
        public string Cpf { get; set; }
        public string Crefito { get; set; }
        public string Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public string Especialidade { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; } = true;

        public Guid UsuarioId { get; set; } // FK para a tabela Usuario
        public virtual Usuario Usuario { get; set; } // Propriedade de navegação para o Profissional associado
    }
}