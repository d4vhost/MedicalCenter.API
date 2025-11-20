using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class EmpleadoUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        // AQUÍ ESTÁ LA CLAVE: Sin [Required], permitiendo nulos o vacíos
        [StringLength(255)]
        public string? Password { get; set; }

        [StringLength(50)]
        public string? Rol { get; set; }

        public int? CentroMedicoId { get; set; }
    }
}
