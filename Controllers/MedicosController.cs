using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCenter.API.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class MedicosController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public MedicosController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Medicos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicoDto>>> GetMedicos()
    {
        return await _context.Medicos
            .Include(m => m.Empleado)
                .ThenInclude(e => e.CentroMedico)
            .Include(m => m.Especialidad)
            .Select(m => new MedicoDto
            {
                Id = m.Id,
                EmpleadoId = m.EmpleadoId,
                // SOLUCIÓN: Comprobamos si Empleado es nulo
                NombreCompleto = m.Empleado != null ? $"{m.Empleado.Nombre} {m.Empleado.Apellido}" : "Empleado no encontrado",
                Cedula = m.Empleado != null ? m.Empleado.Cedula : "N/A",
                EspecialidadId = m.EspecialidadId,
                NombreEspecialidad = m.Especialidad != null ? m.Especialidad.Nombre : "Sin especialidad",
                // SOLUCIÓN: Convertimos de forma segura int? a int (o usamos 0 si es nulo)
                CentroMedicoId = m.Empleado != null ? (m.Empleado.CentroMedicoId ?? 0) : 0,
                // SOLUCIÓN: Comprobamos la cadena de nulos
                NombreCentroMedico = (m.Empleado != null && m.Empleado.CentroMedico != null) ? m.Empleado.CentroMedico.Nombre : "Sin centro médico"
            })
            .ToListAsync();
    }

    // GET: api/Medicos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicoDto>> GetMedico(int id)
    {
        var medico = await _context.Medicos
            .Include(m => m.Empleado)
                .ThenInclude(e => e.CentroMedico)
            .Include(m => m.Especialidad)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (medico == null)
        {
            return NotFound();
        }

        // SOLUCIÓN: Añadimos comprobaciones de nulos aquí también
        var medicoDto = new MedicoDto
        {
            Id = medico.Id,
            EmpleadoId = medico.EmpleadoId,
            NombreCompleto = medico.Empleado != null ? $"{medico.Empleado.Nombre} {medico.Empleado.Apellido}" : "Empleado no encontrado",
            Cedula = medico.Empleado != null ? medico.Empleado.Cedula : "N/A",
            EspecialidadId = medico.EspecialidadId,
            NombreEspecialidad = medico.Especialidad != null ? medico.Especialidad.Nombre : "Sin especialidad",
            CentroMedicoId = medico.Empleado != null ? (medico.Empleado.CentroMedicoId ?? 0) : 0,
            NombreCentroMedico = (medico.Empleado != null && medico.Empleado.CentroMedico != null) ? medico.Empleado.CentroMedico.Nombre : "Sin centro médico"
        };

        return Ok(medicoDto);
    }

    // POST: api/Medicos
    [HttpPost]
    public async Task<ActionResult<MedicoDto>> PostMedico(MedicoCreateDto medicoDto)
    {
        var empleado = await _context.Empleados.FindAsync(medicoDto.EmpleadoId);
        if (empleado == null)
        {
            return BadRequest("El ID del empleado no existe.");
        }

        var especialidad = await _context.Especialidades.FindAsync(medicoDto.EspecialidadId);
        if (especialidad == null)
        {
            return BadRequest("El ID de la especialidad no existe.");
        }

        var nuevoMedico = new Medico
        {
            EmpleadoId = medicoDto.EmpleadoId,
            EspecialidadId = medicoDto.EspecialidadId
        };

        _context.Medicos.Add(nuevoMedico);
        await _context.SaveChangesAsync();

        // Para devolver el DTO completo, cargamos las relaciones necesarias
        await _context.Entry(empleado).Reference(e => e.CentroMedico).LoadAsync();

        var resultadoDto = new MedicoDto
        {
            Id = nuevoMedico.Id,
            EmpleadoId = nuevoMedico.EmpleadoId,
            NombreCompleto = $"{empleado.Nombre} {empleado.Apellido}",
            Cedula = empleado.Cedula,
            EspecialidadId = nuevoMedico.EspecialidadId,
            NombreEspecialidad = especialidad.Nombre,
            CentroMedicoId = empleado.CentroMedicoId ?? 0,
            NombreCentroMedico = empleado.CentroMedico != null ? empleado.CentroMedico.Nombre : "Sin centro médico"
        };

        return CreatedAtAction(nameof(GetMedico), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Medicos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMedico(int id, MedicoCreateDto medicoDto)
    {
        var medico = await _context.Medicos.FindAsync(id);
        if (medico == null)
        {
            return NotFound();
        }

        // Por lo general, no se debería cambiar el empleado de un médico, solo su especialidad
        if (medico.EmpleadoId != medicoDto.EmpleadoId && medicoDto.EmpleadoId != 0)
        {
            return BadRequest("No se puede cambiar el empleado asociado a un médico existente.");
        }

        if (!await _context.Especialidades.AnyAsync(e => e.Id == medicoDto.EspecialidadId))
        {
            return BadRequest("El ID de la especialidad no existe.");
        }

        medico.EspecialidadId = medicoDto.EspecialidadId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Medicos/5
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