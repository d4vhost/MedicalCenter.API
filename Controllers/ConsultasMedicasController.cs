using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCenter.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ConsultasMedicasController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public ConsultasMedicasController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/ConsultasMedicas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConsultaMedicaDto>>> GetConsultasMedicas()
    {
        return await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
                .ThenInclude(m => m.Empleado)
            .Select(c => new ConsultaMedicaDto
            {
                Id = c.Id,
                FechaHora = c.FechaHora,
                PacienteId = c.PacienteId,
                NombrePaciente = c.Paciente != null ? $"{c.Paciente.Nombre} {c.Paciente.Apellido}" : "Paciente no encontrado",
                MedicoId = c.MedicoId,
                // SOLUCIÓN: Cambiamos el operador ?. por una comprobación explícita
                NombreMedico = (c.Medico != null && c.Medico.Empleado != null) ? $"{c.Medico.Empleado.Nombre} {c.Medico.Empleado.Apellido}" : "Médico no encontrado",
                Motivo = c.Motivo
            })
            .ToListAsync();
    }

    // GET: api/ConsultasMedicas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ConsultaMedicaDto>> GetConsultaMedica(int id)
    {
        var consulta = await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
                .ThenInclude(m => m.Empleado)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (consulta == null)
        {
            return NotFound();
        }

        var consultaDto = new ConsultaMedicaDto
        {
            Id = consulta.Id,
            FechaHora = consulta.FechaHora,
            PacienteId = consulta.PacienteId,
            NombrePaciente = consulta.Paciente != null ? $"{consulta.Paciente.Nombre} {consulta.Paciente.Apellido}" : "Paciente no encontrado",
            MedicoId = consulta.MedicoId,
            // SOLUCIÓN: Cambiamos el operador ?. por una comprobación explícita
            NombreMedico = (consulta.Medico != null && consulta.Medico.Empleado != null) ? $"{consulta.Medico.Empleado.Nombre} {consulta.Medico.Empleado.Apellido}" : "Médico no encontrado",
            Motivo = consulta.Motivo
        };

        return Ok(consultaDto);
    }

    // POST: api/ConsultasMedicas
    [HttpPost]
    public async Task<ActionResult<ConsultaMedica>> PostConsultaMedica(ConsultaMedicaCreateDto consultaDto)
    {
        if (!await _context.Pacientes.AnyAsync(p => p.Id == consultaDto.PacienteId))
        {
            return BadRequest("El ID del Paciente no existe.");
        }
        if (!await _context.Medicos.AnyAsync(m => m.Id == consultaDto.MedicoId))
        {
            return BadRequest("El ID del Médico no existe.");
        }

        var nuevaConsulta = new ConsultaMedica
        {
            PacienteId = consultaDto.PacienteId,
            MedicoId = consultaDto.MedicoId,
            Motivo = consultaDto.Motivo,
            FechaHora = consultaDto.FechaHora.HasValue ? consultaDto.FechaHora.Value : DateTime.UtcNow
        };

        _context.ConsultasMedicas.Add(nuevaConsulta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetConsultaMedica), new { id = nuevaConsulta.Id }, nuevaConsulta);
    }

    // PUT: api/ConsultasMedicas/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutConsultaMedica(int id, ConsultaMedicaCreateDto consultaDto)
    {
        var consulta = await _context.ConsultasMedicas.FindAsync(id);
        if (consulta == null)
        {
            return NotFound();
        }

        if (!await _context.Pacientes.AnyAsync(p => p.Id == consultaDto.PacienteId))
        {
            return BadRequest("El ID del Paciente no existe.");
        }
        if (!await _context.Medicos.AnyAsync(m => m.Id == consultaDto.MedicoId))
        {
            return BadRequest("El ID del Médico no existe.");
        }

        consulta.PacienteId = consultaDto.PacienteId;
        consulta.MedicoId = consultaDto.MedicoId;
        consulta.Motivo = consultaDto.Motivo;
        consulta.FechaHora = consultaDto.FechaHora.HasValue ? consultaDto.FechaHora.Value : DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/ConsultasMedicas/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConsultaMedica(int id)
    {
        var consulta = await _context.ConsultasMedicas.FindAsync(id);
        if (consulta == null)
        {
            return NotFound();
        }

        _context.ConsultasMedicas.Remove(consulta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}