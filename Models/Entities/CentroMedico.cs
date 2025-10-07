using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class CentroMedico
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty; 

    [StringLength(255)]
    public string? Direccion { get; set; } 

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}