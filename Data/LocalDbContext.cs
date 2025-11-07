using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Data
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        public DbSet<ConsultaMedica> ConsultasMedicas { get; set; }
        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<Prescripcion> Prescripciones { get; set; }
    }
}
