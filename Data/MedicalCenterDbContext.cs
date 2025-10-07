using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class MedicalCenterDbContext : DbContext
{
    public MedicalCenterDbContext(DbContextOptions<MedicalCenterDbContext> options) : base(options)
    {
    }

    public DbSet<CentroMedico> CentrosMedicos { get; set; }
    public DbSet<Especialidad> Especialidades { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Medico> Medicos { get; set; }
    public DbSet<Paciente> Pacientes { get; set; }
    public DbSet<Medicamento> Medicamentos { get; set; }
    public DbSet<ConsultaMedica> ConsultasMedicas { get; set; }
    public DbSet<Diagnostico> Diagnosticos { get; set; }
    public DbSet<Prescripcion> Prescripciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CentroMedico>().ToTable("centros_medicos");
        modelBuilder.Entity<Especialidad>().ToTable("especialidades");
        modelBuilder.Entity<Empleado>().ToTable("empleados");
        modelBuilder.Entity<Medico>().ToTable("medicos");
        modelBuilder.Entity<Paciente>().ToTable("pacientes");
        modelBuilder.Entity<Medicamento>().ToTable("medicamentos");
        modelBuilder.Entity<ConsultaMedica>().ToTable("consultas_medicas");
        modelBuilder.Entity<Diagnostico>().ToTable("diagnosticos");
        modelBuilder.Entity<Prescripcion>().ToTable("prescripciones");

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.Property(e => e.CentroMedicoId).HasColumnName("centro_medico_id");
        });

        modelBuilder.Entity<Medico>(entity =>
        {
            entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
            entity.Property(e => e.EspecialidadId).HasColumnName("especialidad_id");
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
        });

        modelBuilder.Entity<Medicamento>(entity =>
        {
            entity.Property(e => e.NombreGenerico).HasColumnName("nombre_generico");
            entity.Property(e => e.NombreComercial).HasColumnName("nombre_comercial");
        });

        modelBuilder.Entity<ConsultaMedica>(entity =>
        {
            entity.Property(e => e.FechaHora).HasColumnName("fecha_hora");
            entity.Property(e => e.PacienteId).HasColumnName("paciente_id");
            entity.Property(e => e.MedicoId).HasColumnName("medico_id");
        });

        modelBuilder.Entity<Diagnostico>(entity =>
        {
            entity.Property(e => e.ConsultaId).HasColumnName("consulta_id");
            entity.Property(e => e.EnfermedadNombre).HasColumnName("enfermedad_nombre");
        });

        modelBuilder.Entity<Prescripcion>(entity =>
        {
            entity.Property(e => e.DiagnosticoId).HasColumnName("diagnostico_id");
            entity.Property(e => e.MedicamentoId).HasColumnName("medicamento_id");
        });
    }
}