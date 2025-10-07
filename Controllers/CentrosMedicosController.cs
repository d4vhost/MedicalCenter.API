using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using MedicalCenter.API.Models.DTOs;     


[Route("api/[controller]")]
[ApiController]
public class CentrosMedicosController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;
    public CentrosMedicosController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CentroMedicoDto>>> GetCentrosMedicos()
    {
        var centrosMedicos = await _context.CentrosMedicos
            .Select(cm => new CentroMedicoDto
            {
                Id = cm.Id,
                Nombre = cm.Nombre,
                Direccion = cm.Direccion
            })
            .ToListAsync();

        return Ok(centrosMedicos);
    }
    // Obtiene un centro médico específico por su ID.
    [HttpGet("{id}")]
    public async Task<ActionResult<CentroMedicoDto>> GetCentroMedico(int id)
    {
        var centroMedico = await _context.CentrosMedicos.FindAsync(id);

        if (centroMedico == null)
        {
            return NotFound();
        }
        var centroMedicoDto = new CentroMedicoDto
        {
            Id = centroMedico.Id,
            Nombre = centroMedico.Nombre,
            Direccion = centroMedico.Direccion
        };

        return Ok(centroMedicoDto);
    }

    // Crea un nuevo centro médico.
    [HttpPost]
    public async Task<ActionResult<CentroMedicoDto>> PostCentroMedico(CentroMedicoCreateDto centroMedicoDto)
    {
        var nuevoCentroMedico = new CentroMedico
        {
            Nombre = centroMedicoDto.Nombre,
            Direccion = centroMedicoDto.Direccion
        };

        _context.CentrosMedicos.Add(nuevoCentroMedico);
        await _context.SaveChangesAsync(); 

        var resultadoDto = new CentroMedicoDto
        {
            Id = nuevoCentroMedico.Id,
            Nombre = nuevoCentroMedico.Nombre,
            Direccion = nuevoCentroMedico.Direccion
        };

        return CreatedAtAction(nameof(GetCentroMedico), new { id = resultadoDto.Id }, resultadoDto);
    }

    // Actualiza un centro médico existente.
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCentroMedico(int id, CentroMedicoCreateDto centroMedicoDto)
    {
        var centroMedico = await _context.CentrosMedicos.FindAsync(id);

        if (centroMedico == null)
        {
            return NotFound();
        }
        centroMedico.Nombre = centroMedicoDto.Nombre;
        centroMedico.Direccion = centroMedicoDto.Direccion;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Elimina un centro médico.
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