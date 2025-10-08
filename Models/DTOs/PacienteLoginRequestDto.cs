using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class PacienteLoginRequestDto
    {
        [Required]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        public DateTime FechaNacimiento { get; set; }
    }
}
