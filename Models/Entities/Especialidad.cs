using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Especialidad
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<Medico> Medicos { get; set; } = new List<Medico>(); 
}