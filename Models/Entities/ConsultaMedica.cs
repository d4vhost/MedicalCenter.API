using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ConsultaMedica
{
    [Key]
    public int Id { get; set; }

    public DateTime FechaHora { get; set; }

    public int PacienteId { get; set; }
    [ForeignKey("PacienteId")]
    public virtual Paciente Paciente { get; set; } = null!; 

    public int MedicoId { get; set; }
    [ForeignKey("MedicoId")]
    public virtual Medico Medico { get; set; } = null!; 

    public string? Motivo { get; set; } 
}