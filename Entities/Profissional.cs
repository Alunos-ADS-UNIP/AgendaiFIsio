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
        public string NomeCompleto { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Crefito { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public string Especialidade { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; } = true;

        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!; // Carregado pelo EF quando incluído na consulta.
    }
}
