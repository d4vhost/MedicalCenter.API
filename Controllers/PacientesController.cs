using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class PacientesController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public PacientesController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Pacientes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> GetPacientes()
    {
        var pacientes = await _context.Pacientes
            .Select(p => new PacienteDto
            {
                Id = p.Id,
                Cedula = p.Cedula,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                FechaNacimiento = p.FechaNacimiento,
                Direccion = p.Direccion
            })
            .ToListAsync();

        return Ok(pacientes);
    }

    // GET: api/Pacientes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PacienteDto>> GetPaciente(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);

        if (paciente == null)
        {
            return NotFound();
        }

        var pacienteDto = new PacienteDto
        {
            Id = paciente.Id,
            Cedula = paciente.Cedula,
            Nombre = paciente.Nombre,
            Apellido = paciente.Apellido,
            FechaNacimiento = paciente.FechaNacimiento,
            Direccion = paciente.Direccion
        };

        return Ok(pacienteDto);
    }

    // POST: api/Pacientes
    [HttpPost]
    public async Task<ActionResult<PacienteDto>> PostPaciente(PacienteCreateDto pacienteDto)
    {
        var nuevoPaciente = new Paciente
        {
            Cedula = pacienteDto.Cedula,
            Nombre = pacienteDto.Nombre,
            Apellido = pacienteDto.Apellido,
            FechaNacimiento = pacienteDto.FechaNacimiento,
            Direccion = pacienteDto.Direccion
        };

        _context.Pacientes.Add(nuevoPaciente);
        await _context.SaveChangesAsync();

        var resultadoDto = new PacienteDto
        {
            Id = nuevoPaciente.Id,
            Cedula = nuevoPaciente.Cedula,
            Nombre = nuevoPaciente.Nombre,
            Apellido = nuevoPaciente.Apellido,
            FechaNacimiento = nuevoPaciente.FechaNacimiento,
            Direccion = nuevoPaciente.Direccion
        };

        return CreatedAtAction(nameof(GetPaciente), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Pacientes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPaciente(int id, PacienteCreateDto pacienteDto)
    {
        var paciente = await _context.Pacientes.FindAsync(id);

        if (paciente == null)
        {
            return NotFound();
        }

        paciente.Cedula = pacienteDto.Cedula;
        paciente.Nombre = pacienteDto.Nombre;
        paciente.Apellido = pacienteDto.Apellido;
        paciente.FechaNacimiento = pacienteDto.FechaNacimiento;
        paciente.Direccion = pacienteDto.Direccion;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Pacientes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaciente(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
        {
            return NotFound();
        }

        _context.Pacientes.Remove(paciente);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Pacientes/5/historial
    [HttpGet("{id}/historial")]
    public async Task<ActionResult> GetHistorial(int id)
    {
        var consultas = await _context.ConsultasMedicas
            .Where(c => c.PacienteId == id)
            .Select(c => new { c.Id, c.FechaHora, c.Motivo })
            .ToListAsync();

        var consultaIds = consultas.Select(c => c.Id).ToList();

        var diagnosticos = await _context.Diagnosticos
            .Where(d => consultaIds.Contains(d.ConsultaId))
            .ToListAsync();

        var diagnosticoIds = diagnosticos.Select(d => d.Id).ToList();

        var prescripciones = await _context.Prescripciones
            .Where(p => diagnosticoIds.Contains(p.DiagnosticoId))
            .Include(p => p.Medicamento)
            .Select(p => new
            {
                p.Id,
                p.DiagnosticoId,
                p.Indicaciones,
                NombreMedicamento = p.Medicamento != null ? p.Medicamento.NombreGenerico : "Medicamento no encontrado"
            })
            .ToListAsync();

        return Ok(new { consultas, diagnosticos, prescripciones });
    }
}