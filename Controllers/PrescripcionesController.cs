using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCenter.API.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class PrescripcionesController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public PrescripcionesController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Prescripciones
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrescripcionDto>>> GetPrescripciones()
    {
        return await _context.Prescripciones
            .Include(p => p.Medicamento)
            .Select(p => new PrescripcionDto
            {
                Id = p.Id,
                DiagnosticoId = p.DiagnosticoId,
                MedicamentoId = p.MedicamentoId,
                Indicaciones = p.Indicaciones,
                NombreMedicamento = (p.Medicamento != null) ? p.Medicamento.NombreGenerico : "Medicamento no encontrado"
            })
            .ToListAsync();
    }

    // GET: api/Prescripciones/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PrescripcionDto>> GetPrescripcion(int id)
    {
        var prescripcion = await _context.Prescripciones
            .Include(p => p.Medicamento)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescripcion == null)
        {
            return NotFound();
        }

        var dto = new PrescripcionDto
        {
            Id = prescripcion.Id,
            DiagnosticoId = prescripcion.DiagnosticoId,
            MedicamentoId = prescripcion.MedicamentoId,
            Indicaciones = prescripcion.Indicaciones,
            NombreMedicamento = (prescripcion.Medicamento != null) ? prescripcion.Medicamento.NombreGenerico : "Medicamento no encontrado"
        };

        return Ok(dto);
    }

    // POST: api/Prescripciones
    [HttpPost]
    public async Task<ActionResult<PrescripcionDto>> PostPrescripcion(PrescripcionCreateDto prescripcionDto)
    {
        if (!await _context.Diagnosticos.AnyAsync(d => d.Id == prescripcionDto.DiagnosticoId))
        {
            return BadRequest("El ID del diagnóstico no existe.");
        }
        if (!await _context.Medicamentos.AnyAsync(m => m.Id == prescripcionDto.MedicamentoId))
        {
            return BadRequest("El ID del medicamento no existe.");
        }

        var nuevaPrescripcion = new Prescripcion
        {
            DiagnosticoId = prescripcionDto.DiagnosticoId,
            MedicamentoId = prescripcionDto.MedicamentoId,
            Indicaciones = prescripcionDto.Indicaciones
        };

        _context.Prescripciones.Add(nuevaPrescripcion);
        await _context.SaveChangesAsync();

        await _context.Entry(nuevaPrescripcion).Reference(p => p.Medicamento).LoadAsync();

        var resultadoDto = new PrescripcionDto
        {
            Id = nuevaPrescripcion.Id,
            DiagnosticoId = nuevaPrescripcion.DiagnosticoId,
            MedicamentoId = nuevaPrescripcion.MedicamentoId,
            Indicaciones = nuevaPrescripcion.Indicaciones,
            NombreMedicamento = (nuevaPrescripcion.Medicamento != null) ? nuevaPrescripcion.Medicamento.NombreGenerico : string.Empty
        };

        return CreatedAtAction(nameof(GetPrescripcion), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Prescripciones/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPrescripcion(int id, PrescripcionCreateDto prescripcionDto)
    {
        var prescripcion = await _context.Prescripciones.FindAsync(id);
        if (prescripcion == null)
        {
            return NotFound();
        }

        if (!await _context.Diagnosticos.AnyAsync(d => d.Id == prescripcionDto.DiagnosticoId))
        {
            return BadRequest("El ID del diagnóstico no existe.");
        }
        if (!await _context.Medicamentos.AnyAsync(m => m.Id == prescripcionDto.MedicamentoId))
        {
            return BadRequest("El ID del medicamento no existe.");
        }

        prescripcion.DiagnosticoId = prescripcionDto.DiagnosticoId;
        prescripcion.MedicamentoId = prescripcionDto.MedicamentoId;
        prescripcion.Indicaciones = prescripcionDto.Indicaciones;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Prescripciones/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrescripcion(int id)
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