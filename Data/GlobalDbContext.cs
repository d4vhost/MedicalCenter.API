using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Data
{
    public class GlobalDbContext : DbContext
    {
        public GlobalDbContext(DbContextOptions<GlobalDbContext> options) : base(options)
        {
        }

        // Tablas Globales
        public DbSet<CentroMedico> CentrosMedicos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Medico> Medicos { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Medicamento> Medicamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CentroMedico>().ToTable("centros_medicos");
            modelBuilder.Entity<Empleado>().ToTable("empleados");
            modelBuilder.Entity<Especialidad>().ToTable("especialidades");
            modelBuilder.Entity<Medico>().ToTable("medicos");
            modelBuilder.Entity<Paciente>().ToTable("pacientes");
            modelBuilder.Entity<Medicamento>().ToTable("medicamentos");
        }
    }
}