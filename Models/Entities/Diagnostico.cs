using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Diagnostico
{
    [Key]
    public int Id { get; set; }

    public int ConsultaId { get; set; }
    [ForeignKey("ConsultaId")]
    public virtual ConsultaMedica ConsultaMedica { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string EnfermedadNombre { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}