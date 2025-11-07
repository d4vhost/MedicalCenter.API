using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Controllers
{
    [Authorize] // Todos pueden ver
    [Route("api/[controller]")]
    [ApiController]
    public class MedicosController : ControllerBase
    {
        private readonly GlobalDbContext _context; // <--- CONTEXTO GLOBAL

        public MedicosController(GlobalDbContext context)
        {
            _context = context;
        }

        // GET: api/Medicos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Medico>>> GetMedicos()
        {
            // Incluimos Empleado y Especialidad para mostrar info útil
            return await _context.Medicos
                .Include(m => m.Empleado)
                .Include(m => m.Especialidad)
                .ToListAsync();
        }

        // GET: api/Medicos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Medico>> GetMedico(int id)
        {
            var medico = await _context.Medicos
                .Include(m => m.Empleado)
                .Include(m => m.Especialidad)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medico == null)
            {
                return NotFound();
            }

            return medico;
        }

        // PUT: api/Medicos/5
        [Authorize(Roles = "Admin")] // Solo Admin modifica
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMedico(int id, Medico medico)
        {
            if (id != medico.Id)
            {
                return BadRequest();
            }

            _context.Entry(medico).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Medicos.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Medicos
        [Authorize(Roles = "Admin")] // Solo Admin crea
        [HttpPost]
        public async Task<ActionResult<Medico>> PostMedico(Medico medico)
        {
            // Validar que el empleado y la especialidad existan
            var empleadoExiste = await _context.Empleados.AnyAsync(e => e.Id == medico.EmpleadoId);
            var especialidadExiste = await _context.Especialidades.AnyAsync(e => e.Id == medico.EspecialidadId);

            if (!empleadoExiste || !especialidadExiste)
            {
                return BadRequest(new { message = "El EmpleadoId o EspecialidadId no existen." });
            }

            _context.Medicos.Add(medico);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMedico", new { id = medico.Id }, medico);
        }

        // DELETE: api/Medicos/5
        [Authorize(Roles = "Admin")] // Solo Admin borra
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedico(int id)
        {
            var medico = await _context.Medicos.FindAsync(id);
            if (medico == null)
            {
                return NotFound();
            }

            _context.Medicos.Remove(medico);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}