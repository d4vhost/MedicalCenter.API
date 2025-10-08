using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class EmpleadoCreateDto
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

        // --- PROPIEDAD AÑADIDA QUE FALTABA ---
        [Required]
        public string Password { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Rol { get; set; }

        [Required]
        public int CentroMedicoId { get; set; }
    }
}