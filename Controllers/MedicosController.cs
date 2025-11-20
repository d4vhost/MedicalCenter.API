// Archivo: Controllers/MedicosController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
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
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpPut("{id}")]
        // ✅ CORRECCIÓN: Usamos MedicoUpdateDto en lugar de la entidad Medico
        public async Task<IActionResult> PutMedico(int id, MedicoUpdateDto medicoDto)
        {
            if (id != medicoDto.Id)
            {
                return BadRequest("El ID de la URL no coincide con el del cuerpo.");
            }

            // 1. Buscar el médico existente
            var medicoExistente = await _context.Medicos.FindAsync(id);

            if (medicoExistente == null)
            {
                return NotFound();
            }

            // 2. Validar que el nuevo empleado y especialidad existan (Opcional pero recomendado)
            if (!await _context.Empleados.AnyAsync(e => e.Id == medicoDto.EmpleadoId))
                return BadRequest($"El Empleado ID {medicoDto.EmpleadoId} no existe.");

            if (!await _context.Especialidades.AnyAsync(e => e.Id == medicoDto.EspecialidadId))
                return BadRequest($"La Especialidad ID {medicoDto.EspecialidadId} no existe.");

            // 3. Actualizar los campos manualmente
            medicoExistente.EmpleadoId = medicoDto.EmpleadoId;
            medicoExistente.EspecialidadId = medicoDto.EspecialidadId;

            // Entity Framework detecta los cambios automáticamente aquí
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
        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpPost]
        public async Task<ActionResult<Medico>> PostMedico(MedicoCreateDto medicoCreateDto)
        {
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

            var medico = new Medico
            {
                EmpleadoId = medicoCreateDto.EmpleadoId,
                EspecialidadId = medicoCreateDto.EspecialidadId
            };

            _context.Medicos.Add(medico);
            await _context.SaveChangesAsync();

            // Cargar datos completos para la respuesta
            var medicoGuardado = await _context.Medicos
                .Include(m => m.Empleado)
                .Include(m => m.Especialidad)
                .FirstOrDefaultAsync(m => m.Id == medico.Id);

            return CreatedAtAction("GetMedico", new { id = medico.Id }, medicoGuardado);
        }

        // DELETE: api/Medicos/5
        [Authorize(Roles = "ADMINISTRATIVO")]
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