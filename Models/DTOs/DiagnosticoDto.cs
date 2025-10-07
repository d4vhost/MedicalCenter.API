namespace MedicalCenter.API.Models.DTOs
{
    public class DiagnosticoDto
    {
        public int Id { get; set; }
        public int ConsultaId { get; set; }
        public string EnfermedadNombre { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}
