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
                NombreCompleto = m.Empleado.Nombre + " " + m.Empleado.Apellido,
                Cedula = m.Empleado.Cedula,
                EspecialidadId = m.EspecialidadId,
                NombreEspecialidad = m.Especialidad.Nombre,
                CentroMedicoId = m.Empleado.CentroMedicoId,
                NombreCentroMedico = m.Empleado.CentroMedico.Nombre
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

        var medicoDto = new MedicoDto
        {
            Id = medico.Id,
            EmpleadoId = medico.EmpleadoId,
            NombreCompleto = $"{medico.Empleado.Nombre} {medico.Empleado.Apellido}",
            Cedula = medico.Empleado.Cedula,
            EspecialidadId = medico.EspecialidadId,
            NombreEspecialidad = medico.Especialidad.Nombre,
            CentroMedicoId = medico.Empleado.CentroMedicoId,
            NombreCentroMedico = medico.Empleado.CentroMedico.Nombre
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

        await _context.Entry(empleado).Reference(e => e.CentroMedico).LoadAsync();

        var resultadoDto = new MedicoDto
        {
            Id = nuevoMedico.Id,
            EmpleadoId = nuevoMedico.EmpleadoId,
            NombreCompleto = empleado.Nombre + " " + empleado.Apellido,
            Cedula = empleado.Cedula,
            EspecialidadId = nuevoMedico.EspecialidadId,
            NombreEspecialidad = especialidad.Nombre,
            CentroMedicoId = empleado.CentroMedicoId,
            NombreCentroMedico = empleado.CentroMedico.Nombre
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

        if (medico.EmpleadoId != medicoDto.EmpleadoId)
        {
            return BadRequest("No se puede cambiar el empleado asociado a un médico.");
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

