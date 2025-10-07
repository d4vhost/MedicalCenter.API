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

    // GET: api/Diagnosticos
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

    // GET: api/Diagnosticos/5
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

    // POST: api/Diagnosticos
    [HttpPost]
    public async Task<ActionResult<DiagnosticoDto>> PostDiagnostico(DiagnosticoCreateDto diagnosticoDto)
    {
        // Verificamos que la consulta a la que se asocia el diagnóstico exista
        if (!await _context.ConsultasMedicas.AnyAsync(c => c.Id == diagnosticoDto.ConsultaId))
        {
            return BadRequest("El ID de la consulta no existe.");
        }

        var nuevoDiagnostico = new Diagnostico
        {
            ConsultaId = diagnosticoDto.ConsultaId,
            EnfermedadNombre = diagnosticoDto.EnfermedadNombre,
            Observaciones = diagnosticoDto.Observaciones
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

    // PUT: api/Diagnosticos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDiagnostico(int id, DiagnosticoCreateDto diagnosticoDto)
    {
        var diagnostico = await _context.Diagnosticos.FindAsync(id);
        if (diagnostico == null)
        {
            return NotFound();
        }

        // Verifica que el nuevo ConsultaId también sea válido
        if (diagnostico.ConsultaId != diagnosticoDto.ConsultaId && !await _context.ConsultasMedicas.AnyAsync(c => c.Id == diagnosticoDto.ConsultaId))
        {
            return BadRequest("El nuevo ID de la consulta no existe.");
        }

        diagnostico.ConsultaId = diagnosticoDto.ConsultaId;
        diagnostico.EnfermedadNombre = diagnosticoDto.EnfermedadNombre;
        diagnostico.Observaciones = diagnosticoDto.Observaciones;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Diagnosticos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiagnostico(int id)
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