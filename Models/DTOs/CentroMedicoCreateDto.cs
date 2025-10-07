using System.ComponentModel.DataAnnotations;

public class CentroMedicoCreateDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Direccion { get; set; }
}