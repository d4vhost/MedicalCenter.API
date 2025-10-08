using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // Usaremos el ID del empleado como contraseña

        [Required]
        public int CentroMedicoId { get; set; }
    }
}
