using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class MedicoCreateDto
    {
        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public int EspecialidadId { get; set; }
    }

}
