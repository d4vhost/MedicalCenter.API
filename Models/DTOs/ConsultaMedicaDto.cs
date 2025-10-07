namespace MedicalCenter.API.Models.DTOs
{
    public class ConsultaMedicaDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int PacienteId { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;
        public int MedicoId { get; set; }
        public string NombreMedico { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }
}
