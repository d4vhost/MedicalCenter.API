using System.ComponentModel.DataAnnotations;

namespace MedicalCenter.API.Models.DTOs
{
    public class MedicoUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public int EspecialidadId { get; set; }
    }
}
