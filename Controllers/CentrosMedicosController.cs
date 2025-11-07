// Archivo: Controllers/CentrosMedicosController.cs

using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Controllers
{
    [Authorize] // Requiere estar logueado
    [Route("api/[controller]")]
    [ApiController]
    public class CentrosMedicosController : ControllerBase
    {
        // CAMBIO: Inyectar GlobalDbContext
        private readonly GlobalDbContext _context;

        public CentrosMedicosController(GlobalDbContext context)
        {
            _context = context;
        }

        // GET: api/CentrosMedicos (Todos los roles pueden ver)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CentroMedico>>> GetCentrosMedicos()
        {
            return await _context.CentrosMedicos.ToListAsync();
        }

        // GET: api/CentrosMedicos/5 (Todos los roles pueden ver)
        [HttpGet("{id}")]
        public async Task<ActionResult<CentroMedico>> GetCentroMedico(int id)
        {
            var centroMedico = await _context.CentrosMedicos.FindAsync(id);

            if (centroMedico == null)
            {
                return NotFound();
            }

            return centroMedico;
        }

        // PUT: api/CentrosMedicos/5 (Solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCentroMedico(int id, CentroMedico centroMedico)
        {
            if (id != centroMedico.Id)
            {
                return BadRequest();
            }

            _context.Entry(centroMedico).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CentrosMedicos.Any(e => e.Id == id))
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

        // POST: api/CentrosMedicos (Solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CentroMedico>> PostCentroMedico(CentroMedico centroMedico)
        {
            _context.CentrosMedicos.Add(centroMedico);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCentroMedico", new { id = centroMedico.Id }, centroMedico);
        }

        // DELETE: api/CentrosMedicos/5 (Solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCentroMedico(int id)
        {
            var centroMedico = await _context.CentrosMedicos.FindAsync(id);
            if (centroMedico == null)
            {
                return NotFound();
            }

            _context.CentrosMedicos.Remove(centroMedico);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}