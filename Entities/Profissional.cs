using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        public string Bio { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; } = true;

        // Vínculo com o catálogo. Fica nulo até o profissional escolher uma opção (no cadastro
        // ou depois, editando o perfil) — ver PUT api/profissional/especialidade.
        public Guid? EspecialidadeId { get; set; }
        public virtual Especialidade? Especialidade { get; set; }

        // Texto livre do cadastro antigo (antes da Task 3). Mantido só para auditoria da
        // migração que associou os textos legados ao catálogo; nenhum fluxo novo lê ou grava
        // este campo. Reaproveita a coluna "Especialidade" já existente no banco.
        [Column("Especialidade")]
        public string EspecialidadeTextoLegado { get; set; } = string.Empty;

        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}