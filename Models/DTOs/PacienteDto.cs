namespace MedicalCenter.API.Models.DTOs
{
    public class PacienteDto
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public DateTime? FechaNacimiento { get; set; } // ✅ Cambiado a nullable
        public string? Direccion { get; set; }
        public string? CentroMedico { get; set; }
        public string? Estado { get; set; }
    }
}