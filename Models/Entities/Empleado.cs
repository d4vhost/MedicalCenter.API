using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Empleado
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Cedula { get; set; } = string.Empty; 

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty; 

    [Required]
    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty; 

    [StringLength(50)]
    public string? Rol { get; set; } 

    public int CentroMedicoId { get; set; }
    [ForeignKey("CentroMedicoId")]
    public virtual CentroMedico CentroMedico { get; set; } = null!; 
}