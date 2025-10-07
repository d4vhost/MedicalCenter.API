using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class MedicamentoCreateDto
    {
        [Required]
        [StringLength(255)]
        public string NombreGenerico { get; set; } = string.Empty;

        [StringLength(255)]
        public string? NombreComercial { get; set; }

        [StringLength(100)]
        public string? Laboratorio { get; set; }
    }
}
