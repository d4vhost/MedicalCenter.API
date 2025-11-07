using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalCenter.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticosController : ControllerBase
    {
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
        [HttpGet("PorConsulta/{consultaId}")]
        public async Task<ActionResult<IEnumerable<Diagnostico>>> GetDiagnosticosPorConsulta(int consultaId)
        {
            using (var _context = GetContextFromToken())
            {
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
                _context.Entry(diagnostico).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Diagnosticos.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
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
        [Authorize(Roles = "Admin")] // Solo Admin borra
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