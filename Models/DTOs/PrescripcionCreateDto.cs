using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class PrescripcionCreateDto
    {
        [Required]
        public int DiagnosticoId { get; set; }

        [Required]
        public int MedicamentoId { get; set; }
        public string? Indicaciones { get; set; }
    }
}
