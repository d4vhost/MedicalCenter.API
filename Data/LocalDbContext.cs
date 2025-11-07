using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Data
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        // Tablas Locales
        public DbSet<ConsultaMedica> ConsultasMedicas { get; set; }
        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<Prescripcion> Prescripciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configuraciones para las tablas locales
        }
    }
}