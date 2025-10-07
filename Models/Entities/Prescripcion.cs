using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Prescripcion
{
    [Key]
    public int Id { get; set; }

    public int DiagnosticoId { get; set; }
    [ForeignKey("DiagnosticoId")]
    public virtual Diagnostico Diagnostico { get; set; } = null!; 

    public int MedicamentoId { get; set; }
    [ForeignKey("MedicamentoId")]
    public virtual Medicamento Medicamento { get; set; } = null!; 

    public string? Indicaciones { get; set; }
}