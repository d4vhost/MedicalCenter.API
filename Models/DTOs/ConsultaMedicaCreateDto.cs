using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class ConsultaMedicaCreateDto
    {
        [Required]
        public int PacienteId { get; set; }

        [Required]
        public int MedicoId { get; set; }
        public string? Motivo { get; set; }
        public DateTime? FechaHora { get; set; }
    }
}
