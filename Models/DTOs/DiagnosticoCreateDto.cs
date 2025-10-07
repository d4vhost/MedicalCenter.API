using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class DiagnosticoCreateDto
    {
        [Required]
        public int ConsultaId { get; set; }

        [Required]
        [StringLength(255)]
        public string EnfermedadNombre { get; set; } = string.Empty;

        public string? Observaciones { get; set; }
    }
}
