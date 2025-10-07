using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class PacienteCreateDto
    {
        [Required]
        [StringLength(10)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(255)]
        public string? Direccion { get; set; }
    }

}
