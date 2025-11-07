namespace MedicalCenter.API.Models.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int CentroMedicoId { get; set; }
    }

}
