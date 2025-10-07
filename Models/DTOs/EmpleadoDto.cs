namespace MedicalCenter.API.Models.DTOs
{
    public class EmpleadoDto
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Rol { get; set; }
        public int CentroMedicoId { get; set; }
        public string? NombreCentroMedico { get; set; }
    }
}
