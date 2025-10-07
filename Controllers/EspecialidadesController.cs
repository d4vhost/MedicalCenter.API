using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class EspecialidadesController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public EspecialidadesController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Especialidades
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EspecialidadDto>>> GetEspecialidades()
    {
        return await _context.Especialidades
            .Select(e => new EspecialidadDto
            {
                Id = e.Id,
                Nombre = e.Nombre
            })
            .ToListAsync();
    }

    // GET: api/Especialidades/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EspecialidadDto>> GetEspecialidad(int id)
    {
        var especialidad = await _context.Especialidades.FindAsync(id);

        if (especialidad == null)
        {
            return NotFound();
        }

        var especialidadDto = new EspecialidadDto
        {
            Id = especialidad.Id,
            Nombre = especialidad.Nombre
        };

        return especialidadDto;
    }

    // POST: api/Especialidades
    [HttpPost]
    public async Task<ActionResult<EspecialidadDto>> PostEspecialidad(EspecialidadCreateDto especialidadDto)
    {
        var nuevaEspecialidad = new Especialidad
        {
            Nombre = especialidadDto.Nombre
        };

        _context.Especialidades.Add(nuevaEspecialidad);
        await _context.SaveChangesAsync();

        var resultadoDto = new EspecialidadDto
        {
            Id = nuevaEspecialidad.Id,
            Nombre = nuevaEspecialidad.Nombre
        };

        return CreatedAtAction(nameof(GetEspecialidad), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Especialidades/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEspecialidad(int id, EspecialidadCreateDto especialidadDto)
    {
        var especialidad = await _context.Especialidades.FindAsync(id);

        if (especialidad == null)
        {
            return NotFound();
        }

        especialidad.Nombre = especialidadDto.Nombre;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Especialidades.Any(e => e.Id == id))
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

    // DELETE: api/Especialidades/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEspecialidad(int id)
    {
        var especialidad = await _context.Especialidades.FindAsync(id);
        if (especialidad == null)
        {
            return NotFound();
        }

        _context.Especialidades.Remove(especialidad);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
