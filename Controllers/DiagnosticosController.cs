using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.API.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
public class DiagnosticosController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public DiagnosticosController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiagnosticoDto>>> GetDiagnosticos()
    {
        return await _context.Diagnosticos
            .Select(d => new DiagnosticoDto
            {
                Id = d.Id,
                ConsultaId = d.ConsultaId,
                EnfermedadNombre = d.EnfermedadNombre,
                Observaciones = d.Observaciones
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DiagnosticoDto>> GetDiagnostico(int id)
    {
        var diagnostico = await _context.Diagnosticos.FindAsync(id);

        if (diagnostico == null)
        {
            return NotFound();
        }

        var dto = new DiagnosticoDto
        {
            Id = diagnostico.Id,
            ConsultaId = diagnostico.ConsultaId,
            EnfermedadNombre = diagnostico.EnfermedadNombre,
            Observaciones = diagnostico.Observaciones
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<DiagnosticoDto>> PostDiagnostico(DiagnosticoCreateDto diagnosticoDto)
    {
        if (!await _context.ConsultasMedicas.AnyAsync(c => c.Id == diagnosticoDto.ConsultaId))
        {
            return BadRequest("EL ID DE LA CONSULTA NO EXISTE.");
        }

        var nuevoDiagnostico = new Diagnostico
        {
            ConsultaId = diagnosticoDto.ConsultaId,
            EnfermedadNombre = diagnosticoDto.EnfermedadNombre.ToUpper(), // Guardar en mayúsculas
            Observaciones = diagnosticoDto.Observaciones?.ToUpper() // Guardar en mayúsculas
        };

        _context.Diagnosticos.Add(nuevoDiagnostico);
        await _context.SaveChangesAsync();

        var resultadoDto = new DiagnosticoDto
        {
            Id = nuevoDiagnostico.Id,
            ConsultaId = nuevoDiagnostico.ConsultaId,
            EnfermedadNombre = nuevoDiagnostico.EnfermedadNombre,
            Observaciones = nuevoDiagnostico.Observaciones
        };

        return CreatedAtAction(nameof(GetDiagnostico), new { id = resultadoDto.Id }, resultadoDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutDiagnostico(int id, DiagnosticoCreateDto diagnosticoDto)
    {
        var diagnostico = await _context.Diagnosticos.FindAsync(id);
        if (diagnostico == null)
        {
            return NotFound();
        }

        if (diagnostico.ConsultaId != diagnosticoDto.ConsultaId && !await _context.ConsultasMedicas.AnyAsync(c => c.Id == diagnosticoDto.ConsultaId))
        {
            return BadRequest("EL NUEVO ID DE LA CONSULTA NO EXISTE.");
        }

        diagnostico.ConsultaId = diagnosticoDto.ConsultaId;
        diagnostico.EnfermedadNombre = diagnosticoDto.EnfermedadNombre.ToUpper(); // Guardar en mayúsculas
        diagnostico.Observaciones = diagnosticoDto.Observaciones?.ToUpper(); // Guardar en mayúsculas

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiagnostico(int id)
    {
        var diagnostico = await _context.Diagnosticos
                                        .Include(d => d.Prescripciones) // Incluir prescripciones
                                        .FirstOrDefaultAsync(d => d.Id == id);
        if (diagnostico == null)
        {
            return NotFound();
        }

        // Eliminar prescripciones asociadas primero
        _context.Prescripciones.RemoveRange(diagnostico.Prescripciones);

        // Luego eliminar el diagnóstico
        _context.Diagnosticos.Remove(diagnostico);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Loggear el error si es necesario
            Console.WriteLine($"ERROR AL ELIMINAR DIAGNÓSTICO {id}: {ex.InnerException?.Message ?? ex.Message}");
            return StatusCode(500, "ERROR INTERNO AL ELIMINAR EL DIAGNÓSTICO Y SUS PRESCRIPCIONES.");
        }


        return NoContent();
    }
}