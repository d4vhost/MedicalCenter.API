namespace MedicalCenter.API.Models.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty; // En un futuro, sería un JWT real
        public int EmpleadoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Rol { get; set; }
    }

}
