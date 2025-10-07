using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Medico
{
    [Key]
    public int Id { get; set; }

    public int EmpleadoId { get; set; }
    [ForeignKey("EmpleadoId")]
    public virtual Empleado Empleado { get; set; } = null!; 

    public int EspecialidadId { get; set; }
    [ForeignKey("EspecialidadId")]
    public virtual Especialidad Especialidad { get; set; } = null!; 
}