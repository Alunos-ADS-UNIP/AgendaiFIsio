using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace AgendaiFisio.Context
{
    public class AgendaiFisioDbContext : DbContext
    {
        public AgendaiFisioDbContext(DbContextOptions<AgendaiFisioDbContext> options) : base(options)
        {
        }

        public DbSet<AgendaiFisio.Entities.Usuario> Usuarios { get; set; }
        public DbSet<AgendaiFisio.Entities.Paciente> Pacientes { get; set; }
        public DbSet<AgendaiFisio.Entities.Profissional> Profissionais { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("AgendaiFisio");

            base.OnModelCreating(modelBuilder);
        }
        
    }
}