using System.ComponentModel.DataAnnotations;

public class Medicamento
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string NombreGenerico { get; set; } = string.Empty; 

    [StringLength(255)]
    public string? NombreComercial { get; set; } 

    [StringLength(100)]
    public string? Laboratorio { get; set; } 
}