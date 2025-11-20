using MedicalCenter.API.Models.Entities; // ✨ Agregar este using
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Diagnostico
{
    [Key]
    public int Id { get; set; }

    public int ConsultaId { get; set; }

    [ForeignKey("ConsultaId")]
    // ✨ Especificar el tipo completo si es necesario
    public virtual MedicalCenter.API.Models.Entities.ConsultaMedica? ConsultaMedica { get; set; }

    [Required]
    [StringLength(255)]
    public string EnfermedadNombre { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public virtual ICollection<Prescripcion> Prescripciones { get; set; } = new List<Prescripcion>();
}