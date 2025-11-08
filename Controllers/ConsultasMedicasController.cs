// Archivo: Controllers/ConsultasMedicasController.cs
using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;

namespace MedicalCenter.API.Controllers
{
    // ✨ CAMBIO: Quitado [Authorize] de aquí
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultasMedicasController : ControllerBase
    {
        private readonly ILocalDbContextFactory _localContextFactory;
        private readonly GlobalDbContext _globalContext;

        public ConsultasMedicasController(ILocalDbContextFactory localContextFactory, GlobalDbContext globalContext)
        {
            _localContextFactory = localContextFactory;
            _globalContext = globalContext;
        }

        // --- Tus Helpers (están bien) ---
        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
                return null;
            return centroId;
        }
        private LocalDbContext GetContextFromToken(int centroId)
        {
            return _localContextFactory.CreateDbContext(centroId);
        }
        // --- Fin Helpers ---

        // GET: api/ConsultasMedicas
        // ✨ CAMBIO: Ser explícitos con los roles
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultaMedica>>> GetConsultasMedicas()
        {
            // ... (Tu lógica está bien)
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
                return Unauthorized("Token no contiene 'centro_medico_id'.");
            if (centroId.Value == 1)
                return Ok(new List<ConsultaMedica>()); // Admin global ve lista vacía (correcto)

            using (var _context = GetContextFromToken(centroId.Value))
            {
                return await _context.ConsultasMedicas
                                     .OrderByDescending(c => c.FechaHora)
                                     .ToListAsync();
            }
        }

        // GET: api/ConsultasMedicas/5
        // ✨ CAMBIO: Ser explícitos con los roles
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ConsultaMedica>> GetConsultaMedica(int id)
        {
            // ... (Tu lógica está bien)
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
                return Unauthorized("Token no contiene 'centro_medico_id'.");
            if (centroId.Value == 1)
                return NotFound("Consulta no encontrada.");

            using (var _context = GetContextFromToken(centroId.Value))
            {
                var consultaMedica = await _context.ConsultasMedicas.FindAsync(id);
                if (consultaMedica == null)
                    return NotFound("Consulta no encontrada en este centro médico.");
                return consultaMedica;
            }
        }

        // POST: api/ConsultasMedicas
        // ✨ CAMBIO: Ser explícitos con los roles
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPost]
        public async Task<ActionResult<ConsultaMedica>> PostConsultaMedica(ConsultaMedica consultaMedica)
        {
            // ... (Tu lógica está bien)
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
                return Unauthorized("Token no contiene 'centro_medico_id'.");
            if (centroId.Value == 1)
                return Forbid("Admin global no puede crear consultas locales.");

            var pacienteExiste = await _globalContext.Pacientes.AnyAsync(p => p.Id == consultaMedica.PacienteId);
            var medicoExiste = await _globalContext.Medicos.AnyAsync(m => m.Id == consultaMedica.MedicoId);
            if (!pacienteExiste || !medicoExiste)
                return BadRequest(new { message = "El PacienteId o MedicoId no existen." });

            using (var _context = GetContextFromToken(centroId.Value))
            {
                if (consultaMedica.FechaHora == default)
                    consultaMedica.FechaHora = DateTime.UtcNow;
                _context.ConsultasMedicas.Add(consultaMedica);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetConsultaMedica", new { id = consultaMedica.Id }, consultaMedica);
            }
        }

        // PUT: api/ConsultasMedicas/5
        // ✨ CAMBIO: Ser explícitos con los roles
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConsultaMedica(int id, ConsultaMedica consultaMedica)
        {
            // ... (Tu lógica está bien)
            if (id != consultaMedica.Id)
                return BadRequest();

            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
                return Unauthorized("Token no contiene 'centro_medico_id'.");
            if (centroId.Value == 1)
                return Forbid("Admin global no puede modificar consultas locales.");

            using (var _context = GetContextFromToken(centroId.Value))
            {
                var existe = await _context.ConsultasMedicas.AnyAsync(e => e.Id == id);
                if (!existe)
                    return NotFound("La consulta no existe en este centro médico.");

                _context.Entry(consultaMedica).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        // DELETE: api/ConsultasMedicas/5
        [Authorize(Roles = "ADMINISTRATIVO")] // Solo Admin borra
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultaMedica(int id)
        {
            // ... (Tu lógica está bien)
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
                return Unauthorized("Token no contiene 'centro_medico_id'.");
            if (centroId.Value == 1)
                return Forbid("Admin global no puede eliminar consultas locales.");

            using (var _context = GetContextFromToken(centroId.Value))
            {
                var consultaMedica = await _context.ConsultasMedicas.FindAsync(id);
                if (consultaMedica == null)
                    return NotFound();

                _context.ConsultasMedicas.Remove(consultaMedica);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }
    }
}