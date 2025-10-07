namespace MedicalCenter.API.Models.DTOs
{
    public class MedicamentoDto
    {
        public int Id { get; set; }
        public string NombreGenerico { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? Laboratorio { get; set; }
    }

}
