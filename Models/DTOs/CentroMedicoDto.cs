namespace MedicalCenter.API.Models.DTOs;
public class CentroMedicoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
}