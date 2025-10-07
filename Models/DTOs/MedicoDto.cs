namespace MedicalCenter.API.Models.DTOs
{
    public class MedicoDto
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;

        public int EspecialidadId { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;

        public int CentroMedicoId { get; set; }
        public string NombreCentroMedico { get; set; } = string.Empty;
    }

}
