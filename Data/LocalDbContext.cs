// Archivo: Data/LocalDbContext.cs
using MedicalCenter.API.Models.Entities;
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

        // --- ¡AÑADE ESTE MÉTODO! ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de la tabla consultas_medicas
            modelBuilder.Entity<ConsultaMedica>(entity =>
            {
                entity.ToTable("consultas_medicas");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FechaHora).HasColumnName("fecha_hora");
                entity.Property(e => e.PacienteId).HasColumnName("paciente_id");
                entity.Property(e => e.MedicoId).HasColumnName("medico_id");
                entity.Property(e => e.Motivo).HasColumnName("motivo");
            });

            // Mapeo de la tabla diagnosticos
            modelBuilder.Entity<Diagnostico>(entity =>
            {
                entity.ToTable("diagnosticos");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConsultaId).HasColumnName("consulta_id");
                entity.Property(e => e.EnfermedadNombre).HasColumnName("enfermedad_nombre");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            });

            // Mapeo de la tabla prescripciones
            modelBuilder.Entity<Prescripcion>(entity =>
            {
                entity.ToTable("prescripciones");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DiagnosticoId).HasColumnName("diagnostico_id");
                entity.Property(e => e.MedicamentoId).HasColumnName("medicamento_id");
                entity.Property(e => e.Indicaciones).HasColumnName("indicaciones");
            });
        }
    }
}