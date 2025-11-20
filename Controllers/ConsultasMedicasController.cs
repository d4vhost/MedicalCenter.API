using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
using MedicalCenter.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MedicalCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsultasMedicasController : ControllerBase
    {
        private readonly ILocalDbContextFactory _localContextFactory;
        private readonly GlobalDbContext _globalContext;

        public ConsultasMedicasController(ILocalDbContextFactory localContextFactory, GlobalDbContext globalContext)
        {
            _localContextFactory = localContextFactory;
            _globalContext = globalContext;
        }

        // --- HELPERS ---
        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User?.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
                return null;
            return centroId;
        }

        private LocalDbContext GetContextFromToken(int centroId)
        {
            return _localContextFactory.CreateDbContext(centroId);
        }
        // --- FIN HELPERS ---

        // GET: api/ConsultasMedicas
        [HttpGet]
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        public async Task<ActionResult<IEnumerable<ConsultaMedica>>> GetConsultasMedicas()
        {
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue) return Unauthorized("Token inválido: falta centro_medico_id.");
            if (centroId.Value == 1) return Ok(new List<ConsultaMedica>());

            using (var context = GetContextFromToken(centroId.Value))
            {
                return await context.ConsultasMedicas
                                    .OrderByDescending(c => c.FechaHora)
                                    .ToListAsync();
            }
        }

        // GET: api/ConsultasMedicas/5
        [HttpGet("{id}")]
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        public async Task<ActionResult<ConsultaMedica>> GetConsultaMedica(int id)
        {
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue) return Unauthorized();
            if (centroId.Value == 1) return NotFound();

            using (var context = GetContextFromToken(centroId.Value))
            {
                var consulta = await context.ConsultasMedicas.FindAsync(id);
                if (consulta == null)
                {
                    return NotFound("Consulta no encontrada en este centro médico.");
                }
                return consulta;
            }
        }

        [HttpPost]
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        public async Task<ActionResult<ConsultaMedica>> PostConsultaMedica(ConsultaMedicaCreateDto consultaDto)
        {
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue) return Unauthorized();
            if (centroId.Value == 1)
                return Forbid("El administrador global no puede crear consultas médicas locales.");

            // 1. VALIDACIÓN MANUAL EN GLOBAL
            var pacienteExiste = await _globalContext.Pacientes.AnyAsync(p => p.Id == consultaDto.PacienteId);
            if (!pacienteExiste)
                return BadRequest($"El Paciente con ID {consultaDto.PacienteId} no existe en la base global.");

            var medicoExiste = await _globalContext.Medicos.AnyAsync(m => m.Id == consultaDto.MedicoId);
            if (!medicoExiste)
                return BadRequest($"El Médico con ID {consultaDto.MedicoId} no existe en la base global.");

            // 2. GUARDADO EN LOCAL
            using (var context = GetContextFromToken(centroId.Value))
            {
                // Crear instancia sin especificar el namespace completo
                // porque ya lo importamos arriba
                var nuevaConsulta = new ConsultaMedica
                {
                    PacienteId = consultaDto.PacienteId,
                    MedicoId = consultaDto.MedicoId,
                    Motivo = consultaDto.Motivo,
                    FechaHora = consultaDto.FechaHora ?? DateTime.Now
                };

                context.ConsultasMedicas.Add(nuevaConsulta);
                await context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetConsultaMedica), new { id = nuevaConsulta.Id }, nuevaConsulta);
            }
        }

        // PUT: api/ConsultasMedicas/5
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        public async Task<IActionResult> PutConsultaMedica(int id, ConsultaMedica consultaMedica)
        {
            if (id != consultaMedica.Id) return BadRequest("El ID de la URL no coincide con el cuerpo.");

            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue) return Unauthorized();
            if (centroId.Value == 1) return Forbid();

            using (var context = GetContextFromToken(centroId.Value))
            {
                var existe = await context.ConsultasMedicas.AnyAsync(e => e.Id == id);
                if (!existe) return NotFound();

                context.Entry(consultaMedica).State = EntityState.Modified;

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await context.ConsultasMedicas.AnyAsync(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ConsultasMedicas/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMINISTRATIVO")]
        public async Task<IActionResult> DeleteConsultaMedica(int id)
        {
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue) return Unauthorized();
            if (centroId.Value == 1) return Forbid();

            using (var context = GetContextFromToken(centroId.Value))
            {
                var consulta = await context.ConsultasMedicas.FindAsync(id);
                if (consulta == null) return NotFound();

                context.ConsultasMedicas.Remove(consulta);
                await context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}