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
        // **NUEVO: Validar Cédula Duplicada**
        if (await _context.Pacientes.AnyAsync(p => p.Cedula == pacienteDto.Cedula))
        {
            return BadRequest("La cédula ingresada ya pertenece a otro paciente.");
        }

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

        // **NUEVO: Validar Cédula Duplicada al Editar (si se cambia la cédula)**
        if (paciente.Cedula != pacienteDto.Cedula && await _context.Pacientes.AnyAsync(p => p.Cedula == pacienteDto.Cedula && p.Id != id))
        {
            return BadRequest("La nueva cédula ingresada ya pertenece a otro paciente.");
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

        // --- INICIO: Eliminación en Cascada por Código ---
        var consultas = await _context.ConsultasMedicas
            .Where(c => c.PacienteId == id)
            .Include(c => c.Diagnosticos)
                .ThenInclude(d => d.Prescripciones)
            .ToListAsync();

        if (consultas.Any())
        {
            foreach (var consulta in consultas)
            {
                foreach (var diagnostico in consulta.Diagnosticos)
                {
                    _context.Prescripciones.RemoveRange(diagnostico.Prescripciones);
                }
                _context.Diagnosticos.RemoveRange(consulta.Diagnosticos);
            }
            _context.ConsultasMedicas.RemoveRange(consultas);
        }
        // --- FIN: Eliminación en Cascada por Código ---

        _context.Pacientes.Remove(paciente);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Error al eliminar paciente: {ex.InnerException?.Message ?? ex.Message}");
            return StatusCode(500, "Ocurrió un error al intentar eliminar el paciente y su historial relacionado.");
        }

        return NoContent();
    }

    // GET: api/Pacientes/5/historial
    [HttpGet("{id}/historial")]
    public async Task<ActionResult> GetHistorial(int id)
    {
        if (!await _context.Pacientes.AnyAsync(p => p.Id == id))
        {
            return NotFound("Paciente no encontrado.");
        }

        var consultas = await _context.ConsultasMedicas
            .Where(c => c.PacienteId == id)
            .OrderByDescending(c => c.FechaHora)
            .Select(c => new { c.Id, c.FechaHora, c.Motivo })
            .ToListAsync();

        var consultaIds = consultas.Select(c => c.Id).ToList();

        var diagnosticos = await _context.Diagnosticos
            .Where(d => consultaIds.Contains(d.ConsultaId))
            .Select(d => new { d.Id, d.ConsultaId, d.EnfermedadNombre, d.Observaciones })
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

    // GET: api/Pacientes/verificar/{cedula}  <--- ¡RUTA CAMBIADA AQUÍ!
    [HttpGet("verificar/{cedula}")] // Cambio: Renombrada la ruta para evitar conflicto
    public async Task<IActionResult> ExisteCedula(string cedula)
    {
        // Este método solo verifica si existe o no, útil para validaciones rápidas
        var existe = await _context.Pacientes.AnyAsync(p => p.Cedula == cedula);
        if (existe)
        {
            return Ok(); // Devuelve 200 OK si existe
        }
        else
        {
            return NotFound(); // Devuelve 404 Not Found si no existe
        }
    }

    // GET: api/Pacientes/existe/{cedula} <--- Esta ruta se mantiene
    [HttpGet("existe/{cedula}")]
    public async Task<ActionResult<object>> VerificarCedulaExistente(string cedula)
    {
        // Este método devuelve el ID si existe, útil para saber a quién pertenece
        var paciente = await _context.Pacientes
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(p => p.Cedula == cedula);

        if (paciente == null)
        {
            return NotFound(); // Devuelve 404 si NO existe
        }

        // Devuelve 200 OK con el ID del paciente si existe
        return Ok(new { id = paciente.Id });
    }
}