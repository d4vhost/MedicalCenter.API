// Archivo: Controllers/PacientesController.cs
using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Necesario para leer el token

namespace MedicalCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly GlobalDbContext _context;
        // ✨ NUEVO: Inyectamos la fábrica para acceder a Consultas/Diagnósticos (Base Local)
        private readonly ILocalDbContextFactory _localContextFactory;

        public PacientesController(GlobalDbContext context, ILocalDbContextFactory localContextFactory)
        {
            _context = context;
            _localContextFactory = localContextFactory;
        }

        // --- HELPER PARA OBTENER ID DEL CENTRO ---
        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                return null;
            }
            return centroId;
        }

        // GET: api/Pacientes
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes()
        {
            return await _context.Pacientes.ToListAsync();
        }

        // GET: api/Pacientes/existe/1712345678
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("existe/{cedula}")]
        public async Task<ActionResult<object>> GetPacientePorCedula(string cedula)
        {
            var paciente = await _context.Pacientes
                .Where(p => p.Cedula == cedula)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync();

            // Si no existe, devolvemos OK con null (para no generar error rojo en consola)
            if (paciente == null)
            {
                return Ok(null);
            }

            return Ok(paciente);
        }

        // GET: api/Pacientes/1/historial (EL ENDPOINT QUE TE FALTABA)
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("{id}/historial")]
        public async Task<ActionResult<object>> GetHistorialPaciente(int id)
        {
            // 1. Verificar si el paciente existe en la Global (Esto sí es importante)
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
            {
                return NotFound(new { message = "Paciente no encontrado." });
            }

            // 2. Obtener el ID del centro médico actual
            var centroId = GetCentroIdFromToken();
            if (!centroId.HasValue)
            {
                return Unauthorized(new { message = "Token inválido: No contiene centro_medico_id." });
            }

            // 3. Conectarse a la Base Local para buscar el historial
            using (var localContext = _localContextFactory.CreateDbContext(centroId.Value))
            {
                // A) Obtener Consultas
                var consultas = await localContext.ConsultasMedicas
                    .Where(c => c.PacienteId == id)
                    .OrderByDescending(c => c.FechaHora)
                    .ToListAsync();

                // ✨ SOLUCIÓN DEL ERROR: Si no hay consultas, devolvemos listas vacías, NO un 404.
                if (!consultas.Any())
                {
                    return Ok(new
                    {
                        consultas = new List<object>(),
                        diagnosticos = new List<object>(),
                        prescripciones = new List<object>()
                    });
                }

                // B) Obtener Diagnósticos (basado en las consultas encontradas)
                var consultaIds = consultas.Select(c => c.Id).ToList();
                var diagnosticos = await localContext.Diagnosticos
                    .Where(d => consultaIds.Contains(d.ConsultaId))
                    .ToListAsync();

                // C) Obtener Prescripciones (basado en los diagnósticos encontrados)
                var diagnosticoIds = diagnosticos.Select(d => d.Id).ToList();
                var prescripciones = await localContext.Prescripciones
                    .Where(p => diagnosticoIds.Contains(p.DiagnosticoId))
                    .ToListAsync();

                // D) Enriquecer Prescripciones con nombres de medicamentos (Desde Global)
                var medicamentoIds = prescripciones.Select(p => p.MedicamentoId).Distinct().ToList();
                var medicamentosInfo = await _context.Medicamentos
                    .Where(m => medicamentoIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, m => m.NombreGenerico);

                var prescripcionesDto = prescripciones.Select(p => new
                {
                    p.Id,
                    p.DiagnosticoId,
                    p.MedicamentoId,
                    NombreMedicamento = medicamentosInfo.ContainsKey(p.MedicamentoId) ? medicamentosInfo[p.MedicamentoId] : "DESCONOCIDO",
                    p.Indicaciones
                }).ToList();

                // 4. Retornar todo el objeto combinado
                return Ok(new
                {
                    consultas,
                    diagnosticos,
                    prescripciones = prescripcionesDto
                });
            }
        }

        // GET: api/Pacientes/5
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Paciente>> GetPaciente(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
            {
                return NotFound();
            }
            return paciente;
        }

        // PUT: api/Pacientes/5
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaciente(int id, Paciente paciente)
        {
            if (id != paciente.Id)
            {
                return BadRequest();
            }
            _context.Entry(paciente).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pacientes.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        // POST: api/Pacientes
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPost]
        public async Task<ActionResult<Paciente>> PostPaciente(Paciente paciente)
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetPaciente", new { id = paciente.Id }, paciente);
        }

        // DELETE: api/Pacientes/5
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaciente(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
            {
                return NotFound();
            }
            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}