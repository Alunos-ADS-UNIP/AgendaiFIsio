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
        }
        
    }
}