using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Entities;


namespace AgendaiFisio.Context
{
    // Representa as tabelas e relações usadas no banco.
    public class AgendaiFisioDbContext : DbContext
    {
        public AgendaiFisioDbContext(DbContextOptions<AgendaiFisioDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Profissional> Profissionais { get; set; }
        public DbSet<AvaliacaoFisioterapeuta> AvaliacaoFisioterapeuta { get; set; }
        public DbSet<PlanoTerapeutico> PlanosTerapeutico { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Especialidade> Especialidades { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define as relações e regras das tabelas.
            base.OnModelCreating(modelBuilder);

            // Liga uma avaliação a um único plano terapêutico.
            modelBuilder.Entity<AvaliacaoFisioterapeuta>()
                .HasOne(a => a.Plano)
                .WithOne(p => p.AvaliacaoFisioterapeuta)
                .HasForeignKey<PlanoTerapeutico>(p => p.AvaliacaoFisioterapeutaId);

                // Impede dois usuários com o mesmo e-mail.
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Impede dois fisioterapeutas com o mesmo CREFITO, ignorando os cadastros ainda em branco.
            modelBuilder.Entity<Profissional>()
                .HasIndex(p => p.Crefito)
                .IsUnique()
                .HasFilter("[Crefito] <> ''");

            // Impede duas especialidades com o mesmo nome. A collation Latin1_General_CI_AI faz
            // a comparação (e o índice único) ignorar maiúsculas/minúsculas e acentos; o trim e o
            // colapso de espaços internos ficam a cargo do EspecialidadeNomeNormalizador.
            modelBuilder.Entity<Especialidade>(e =>
            {
                e.Property(x => x.Nome)
                    .HasMaxLength(100)
                    .IsRequired()
                    .UseCollation("Latin1_General_CI_AI");
                e.HasIndex(x => x.Nome).IsUnique();

                // Carrega o catálogo inicial aprovado (ver docs/especialidades.md). Reexecutar a
                // migration não duplica nem troca os IDs: o EF só grava a diferença entre esta
                // lista e o que já existe no histórico de migrations.
                e.HasData(EspecialidadeCatalogoSeed.Itens);
            });

            // Liga o profissional a uma especialidade do catálogo. O vínculo é opcional e nunca é
            // apagado em cascata: para remover uma especialidade seria preciso desvincular todos
            // os profissionais primeiro (não há endpoint de DELETE nesta entrega).
            modelBuilder.Entity<Profissional>()
                .HasOne(p => p.Especialidade)
                .WithMany()
                .HasForeignKey(p => p.EspecialidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita apagar avaliações junto com o profissional.
            modelBuilder.Entity<AvaliacaoFisioterapeuta>()
                .HasOne(a => a.Profissional)
                .WithMany()
                .HasForeignKey(a => a.ProfissionalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita apagar avaliações junto com o paciente.
            modelBuilder.Entity<AvaliacaoFisioterapeuta>()
                .HasOne(a => a.Paciente)
                .WithMany()
                .HasForeignKey(a => a.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mantém o histórico de agendamentos mesmo quando há relações no domínio.
            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Paciente)
                .WithMany()
                .HasForeignKey(a => a.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Profissional)
                .WithMany()
                .HasForeignKey(a => a.ProfissionalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita dois agendamentos para o mesmo profissional no mesmo horário.
            modelBuilder.Entity<Agendamento>()
                .HasIndex(a => new { a.ProfissionalId, a.DataHora })
                .IsUnique()
                .HasFilter("[Status] <> 'Cancelado'");

            // Sustenta a paginação do histórico de consultas de um paciente por data.
            modelBuilder.Entity<Agendamento>()
                .HasIndex(a => new { a.PacienteId, a.DataHora });
        }
        
    }
}
