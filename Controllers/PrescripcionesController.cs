// Archivo: Controllers/PrescripcionesController.cs

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
    public class PrescripcionesController : ControllerBase
    {
        // CAMBIO: Inyectar la FÁBRICA
        private readonly ILocalDbContextFactory _localContextFactory;
        // CAMBIO: Inyectar el contexto GLOBAL (para validar Medicamentos)
        private readonly GlobalDbContext _globalContext;

        public PrescripcionesController(ILocalDbContextFactory localContextFactory, GlobalDbContext globalContext)
        {
            _localContextFactory = localContextFactory;
            _globalContext = globalContext;
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


        // GET: api/Prescripciones/PorDiagnostico/5
        // Endpoint útil para tu frontend
        [HttpGet("PorDiagnostico/{diagnosticoId}")]
        public async Task<ActionResult<IEnumerable<Prescripcion>>> GetPrescripcionesPorDiagnostico(int diagnosticoId)
        {
            using (var _context = GetContextFromToken())
            {
                // Validar que el diagnóstico pertenezca a este centro
                var diagnosticoExiste = await _context.Diagnosticos.AnyAsync(d => d.Id == diagnosticoId);
                if (!diagnosticoExiste)
                {
                    return NotFound("El diagnóstico no existe en este centro médico.");
                }

                return await _context.Prescripciones
                    .Where(p => p.DiagnosticoId == diagnosticoId)
                    .ToListAsync();
            }
        }

        // GET: api/Prescripciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Prescripcion>> GetPrescripcion(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var prescripcion = await _context.Prescripciones.FindAsync(id);

                if (prescripcion == null)
                {
                    return NotFound();
                }

                return prescripcion;
            }
        }

        // POST: api/Prescripciones
        [HttpPost]
        public async Task<ActionResult<Prescripcion>> PostPrescripcion(Prescripcion prescripcion)
        {
            // 1. Validar que el Medicamento exista en la DB GLOBAL
            var medicamentoExiste = await _globalContext.Medicamentos.AnyAsync(m => m.Id == prescripcion.MedicamentoId);
            if (!medicamentoExiste)
            {
                return BadRequest(new { message = "El MedicamentoId no existe en la base de datos global." });
            }

            using (var _context = GetContextFromToken())
            {
                // 2. Validar que el Diagnostico exista en la DB LOCAL
                var diagnosticoExiste = await _context.Diagnosticos.AnyAsync(d => d.Id == prescripcion.DiagnosticoId);
                if (!diagnosticoExiste)
                {
                    return BadRequest(new { message = "El DiagnosticoId no existe en este centro médico." });
                }

                // 3. Guardar la prescripción
                _context.Prescripciones.Add(prescripcion);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetPrescripcion", new { id = prescripcion.Id }, prescripcion);
            }
        }

        // DELETE: api/Prescripciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrescripcion(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var prescripcion = await _context.Prescripciones.FindAsync(id);
                if (prescripcion == null)
                {
                    return NotFound();
                }

                _context.Prescripciones.Remove(prescripcion);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }

        // PUT: api/Prescripciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPrescripcion(int id, Prescripcion prescripcion)
        {
            if (id != prescripcion.Id)
            {
                return BadRequest();
            }

            using (var _context = GetContextFromToken())
            {
                var existe = await _context.Prescripciones.AnyAsync(e => e.Id == id);
                if (!existe)
                {
                    return NotFound();
                }

                _context.Entry(prescripcion).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }
    }
}