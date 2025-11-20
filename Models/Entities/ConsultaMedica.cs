using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// Asegúrate de tener este using si usas JsonIgnore
using System.Text.Json.Serialization;

namespace MedicalCenter.API.Models.Entities
{
    [Table("consultas_medicas")]
    public class ConsultaMedica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; }

        [Column("paciente_id")]
        public int PacienteId { get; set; }

        [Column("medico_id")]
        public int MedicoId { get; set; }

        [Column("motivo")]
        public string Motivo { get; set; } = string.Empty;

        [ForeignKey("PacienteId")]
        [JsonIgnore]
        public virtual Paciente? Paciente { get; set; }

        [ForeignKey("MedicoId")]
        [JsonIgnore]
        public virtual Medico? Medico { get; set; }
    }
}