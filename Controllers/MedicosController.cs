// Archivo: Controllers/MedicosController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs; // <-- ✨ PASO 1: Asegúrate de que este 'using' esté presente
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MedicosController : ControllerBase
    {
        private readonly GlobalDbContext _context;

        public MedicosController(GlobalDbContext context)
        {
            _context = context;
        }

        // GET: api/Medicos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Medico>>> GetMedicos()
        {
            // Tu lógica de GET está perfecta (incluye los objetos anidados)
            return await _context.Medicos
                .Include(m => m.Empleado)
                .Include(m => m.Especialidad)
                .ToListAsync();
        }

        // GET: api/Medicos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Medico>> GetMedico(int id)
        {
            // Tu lógica de GET por ID está perfecta
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
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMedico(int id, Medico medico)
        {
            // Esta lógica está bien por ahora, aunque idealmente también usaría un DTO
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

        // --- ✨ INICIO DE LA CORRECCIÓN EN POST ---

        // POST: api/Medicos
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpPost]
        // ✨ PASO 2: Cambia el parámetro de 'Medico medico' a 'MedicoCreateDto medicoCreateDto'
        public async Task<ActionResult<Medico>> PostMedico(MedicoCreateDto medicoCreateDto)
        {
            // ✨ PASO 3: Valida usando los IDs del DTO
            var empleadoExiste = await _context.Empleados.AnyAsync(e => e.Id == medicoCreateDto.EmpleadoId);
            var especialidadExiste = await _context.Especialidades.AnyAsync(e => e.Id == medicoCreateDto.EspecialidadId);

            if (!empleadoExiste)
            {
                return BadRequest(new { message = $"El EmpleadoId {medicoCreateDto.EmpleadoId} no existe." });
            }

            if (!especialidadExiste)
            {
                return BadRequest(new { message = $"El EspecialidadId {medicoCreateDto.EspecialidadId} no existe." });
            }

            // ✨ PASO 4: Crea manualmente la entidad 'Medico' a partir del DTO
            var medico = new Medico
            {
                EmpleadoId = medicoCreateDto.EmpleadoId,
                EspecialidadId = medicoCreateDto.EspecialidadId
            };

            // ✨ PASO 5: Añade la nueva entidad al contexto
            _context.Medicos.Add(medico);
            await _context.SaveChangesAsync();

            // ✨ PASO 6 (Opcional pero recomendado): Carga el médico guardado con sus datos
            // para devolver el objeto completo (tal como lo hace el GET)
            var medicoGuardado = await _context.Medicos
                .Include(m => m.Empleado)
                .Include(m => m.Especialidad)
                .FirstOrDefaultAsync(m => m.Id == medico.Id);

            return CreatedAtAction("GetMedico", new { id = medico.Id }, medicoGuardado);
        }

        // --- ✨ FIN DE LA CORRECCIÓN EN POST ---

        // DELETE: api/Medicos/5
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedico(int id)
        {
            // Tu lógica de DELETE está perfecta
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