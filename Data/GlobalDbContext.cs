// Archivo: Data/GlobalDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Data
{
    public class GlobalDbContext : DbContext
    {
        public GlobalDbContext(DbContextOptions<GlobalDbContext> options) : base(options)
        {
        }

        public DbSet<CentroMedico> CentrosMedicos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Medico> Medicos { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Medicamento> Medicamentos { get; set; }

        // --- ¡AQUÍ ESTÁ LA CORRECCIÓN AMPLIADA! ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de la tabla centros_medicos
            modelBuilder.Entity<CentroMedico>(entity =>
            {
                entity.ToTable("centros_medicos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Direccion).HasColumnName("direccion");
            });

            // Mapeo de la tabla empleados (¡Esta es la que da el error!)
            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("empleados");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Cedula).HasColumnName("cedula");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Apellido).HasColumnName("apellido");
                entity.Property(e => e.Rol).HasColumnName("rol");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.CentroMedicoId).HasColumnName("centro_medico_id"); // <-- El mapeo clave
            });

            // Mapeo de la tabla especialidades
            modelBuilder.Entity<Especialidad>(entity =>
            {
                entity.ToTable("especialidades");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
            });

            // Mapeo de la tabla medicamentos
            modelBuilder.Entity<Medicamento>(entity =>
            {
                entity.ToTable("medicamentos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NombreGenerico).HasColumnName("nombre_generico");
                entity.Property(e => e.NombreComercial).HasColumnName("nombre_comercial");
                entity.Property(e => e.Laboratorio).HasColumnName("laboratorio");
            });

            // Mapeo de la tabla medicos
            modelBuilder.Entity<Medico>(entity =>
            {
                entity.ToTable("medicos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
                entity.Property(e => e.EspecialidadId).HasColumnName("especialidad_id");
            });

            // Mapeo de la tabla pacientes
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("pacientes");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Cedula).HasColumnName("cedula");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Apellido).HasColumnName("apellido");
                entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
                entity.Property(e => e.Direccion).HasColumnName("direccion");
            });
        }
    }
}