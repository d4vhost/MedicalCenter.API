// Archivo: Controllers/DiagnosticosController.cs

using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // <-- ¡¡IMPORTANTE!!

namespace MedicalCenter.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticosController : ControllerBase
    {
        // CAMBIO: Inyectar la FÁBRICA
        private readonly ILocalDbContextFactory _localContextFactory;

        public DiagnosticosController(ILocalDbContextFactory localContextFactory)
        {
            _localContextFactory = localContextFactory;
        }

        // --- Helper para obtener el contexto local correcto ---
        private LocalDbContext GetContextFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                throw new InvalidOperationException("Token de usuario no contiene un 'centro_medico_id' válido.");
            }
            return _localContextFactory.CreateDbContext(centroId);
        }
        // --- Fin del Helper ---

        // GET: api/Diagnosticos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Diagnostico>>> GetDiagnosticos()
        {
            using (var _context = GetContextFromToken())
            {
                return await _context.Diagnosticos.ToListAsync();
            }
        }

        // GET: api/Diagnosticos/PorConsulta/5
        // Endpoint útil para tu frontend
        [HttpGet("PorConsulta/{consultaId}")]
        public async Task<ActionResult<IEnumerable<Diagnostico>>> GetDiagnosticosPorConsulta(int consultaId)
        {
            using (var _context = GetContextFromToken())
            {
                // Validar que la consulta pertenezca a este centro
                var consultaExiste = await _context.ConsultasMedicas.AnyAsync(c => c.Id == consultaId);
                if (!consultaExiste)
                {
                    return NotFound("La consulta no existe en este centro médico.");
                }

                return await _context.Diagnosticos
                    .Where(d => d.ConsultaId == consultaId)
                    .ToListAsync();
            }
        }

        // GET: api/Diagnosticos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Diagnostico>> GetDiagnostico(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var diagnostico = await _context.Diagnosticos.FindAsync(id);

                if (diagnostico == null)
                {
                    return NotFound();
                }

                return diagnostico;
            }
        }

        // PUT: api/Diagnosticos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDiagnostico(int id, Diagnostico diagnostico)
        {
            if (id != diagnostico.Id)
            {
                return BadRequest();
            }

            using (var _context = GetContextFromToken())
            {
                var existe = await _context.Diagnosticos.AnyAsync(e => e.Id == id);
                if (!existe)
                {
                    return NotFound();
                }

                _context.Entry(diagnostico).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        // POST: api/Diagnosticos
        [HttpPost]
        public async Task<ActionResult<Diagnostico>> PostDiagnostico(Diagnostico diagnostico)
        {
            using (var _context = GetContextFromToken())
            {
                // 1. Validar que la Consulta exista EN ESTE CONTEXTO LOCAL
                var consultaExiste = await _context.ConsultasMedicas.AnyAsync(c => c.Id == diagnostico.ConsultaId);
                if (!consultaExiste)
                {
                    return BadRequest(new { message = "El ConsultaId no existe en este centro médico." });
                }

                // 2. Guardar el diagnóstico
                _context.Diagnosticos.Add(diagnostico);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetDiagnostico", new { id = diagnostico.Id }, diagnostico);
            }
        }

        // DELETE: api/Diagnosticos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiagnostico(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var diagnostico = await _context.Diagnosticos.FindAsync(id);
                if (diagnostico == null)
                {
                    return NotFound();
                }

                _context.Diagnosticos.Remove(diagnostico);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }
    }
}