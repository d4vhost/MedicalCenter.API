namespace MedicalCenter.API.Models.DTOs
{
    public class PrescripcionDto
    {
        public int Id { get; set; }
        public int DiagnosticoId { get; set; }
        public int MedicamentoId { get; set; }
        public string? Indicaciones { get; set; }
        public string? NombreMedicamento { get; set; }
    }
}
