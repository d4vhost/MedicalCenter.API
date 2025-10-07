using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.API.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
public class MedicamentosController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public MedicamentosController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Medicamentos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicamentoDto>>> GetMedicamentos()
    {
        return await _context.Medicamentos
            .Select(m => new MedicamentoDto
            {
                Id = m.Id,
                NombreGenerico = m.NombreGenerico,
                NombreComercial = m.NombreComercial,
                Laboratorio = m.Laboratorio
            })
            .ToListAsync();
    }

    // GET: api/Medicamentos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicamentoDto>> GetMedicamento(int id)
    {
        var medicamento = await _context.Medicamentos.FindAsync(id);

        if (medicamento == null)
        {
            return NotFound();
        }

        var dto = new MedicamentoDto
        {
            Id = medicamento.Id,
            NombreGenerico = medicamento.NombreGenerico,
            NombreComercial = medicamento.NombreComercial,
            Laboratorio = medicamento.Laboratorio
        };

        return Ok(dto);
    }

    // POST: api/Medicamentos
    [HttpPost]
    public async Task<ActionResult<MedicamentoDto>> PostMedicamento(MedicamentoCreateDto medicamentoDto)
    {
        var nuevoMedicamento = new Medicamento
        {
            NombreGenerico = medicamentoDto.NombreGenerico,
            NombreComercial = medicamentoDto.NombreComercial,
            Laboratorio = medicamentoDto.Laboratorio
        };

        _context.Medicamentos.Add(nuevoMedicamento);
        await _context.SaveChangesAsync();

        var resultadoDto = new MedicamentoDto
        {
            Id = nuevoMedicamento.Id,
            NombreGenerico = nuevoMedicamento.NombreGenerico,
            NombreComercial = nuevoMedicamento.NombreComercial,
            Laboratorio = nuevoMedicamento.Laboratorio
        };

        return CreatedAtAction(nameof(GetMedicamento), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Medicamentos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMedicamento(int id, MedicamentoCreateDto medicamentoDto)
    {
        var medicamento = await _context.Medicamentos.FindAsync(id);
        if (medicamento == null)
        {
            return NotFound();
        }

        medicamento.NombreGenerico = medicamentoDto.NombreGenerico;
        medicamento.NombreComercial = medicamentoDto.NombreComercial;
        medicamento.Laboratorio = medicamentoDto.Laboratorio;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Medicamentos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedicamento(int id)
    {
        var medicamento = await _context.Medicamentos.FindAsync(id);
        if (medicamento == null)
        {
            return NotFound();
        }

        _context.Medicamentos.Remove(medicamento);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
