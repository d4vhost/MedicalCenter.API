namespace MedicalCenter.API.Models.DTOs
{
    public class PacienteDto
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public DateTime? FechaNacimiento { get; set; }
        public string? Direccion { get; set; }
    }

}
