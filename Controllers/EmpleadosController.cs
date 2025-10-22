using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class EmpleadosController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;

    public EmpleadosController(MedicalCenterDbContext context)
    {
        _context = context;
    }

    // GET: api/Empleados
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpleadoDto>>> GetEmpleados()
    {
        return await _context.Empleados
            .Include(e => e.CentroMedico)
            .Select(e => new EmpleadoDto
            {
                Id = e.Id,
                Cedula = e.Cedula,
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                Rol = e.Rol,
                CentroMedicoId = e.CentroMedicoId,
                NombreCentroMedico = e.CentroMedico != null ? e.CentroMedico.Nombre : "Sin Asignar"
            })
            .ToListAsync();
    }

    // GET: api/Empleados/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EmpleadoDto>> GetEmpleado(int id)
    {
        var empleado = await _context.Empleados
            .Include(e => e.CentroMedico)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (empleado == null)
        {
            return NotFound();
        }

        var empleadoDto = new EmpleadoDto
        {
            Id = empleado.Id,
            Cedula = empleado.Cedula,
            Nombre = empleado.Nombre,
            Apellido = empleado.Apellido,
            Rol = empleado.Rol,
            CentroMedicoId = empleado.CentroMedicoId,
            NombreCentroMedico = empleado.CentroMedico != null ? empleado.CentroMedico.Nombre : "Sin Asignar"
        };

        return empleadoDto;
    }

    // ✅ NUEVO ENDPOINT: Verificar si una cédula existe
    [HttpGet("existe/{cedula}")]
    public async Task<ActionResult<object>> ExisteCedula(string cedula)
    {
        try
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.Cedula == cedula);

            if (empleado == null)
            {
                // La cédula NO existe - devolver 404
                return NotFound(new { message = "Cédula no encontrada" });
            }

            // La cédula SÍ existe - devolver el ID del empleado
            return Ok(new { id = empleado.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al verificar cédula", error = ex.Message });
        }
    }

    // POST: api/Empleados
    [HttpPost]
    public async Task<ActionResult<EmpleadoDto>> PostEmpleado(EmpleadoCreateDto empleadoDto)
    {
        try
        {
            // ✅ VALIDAR QUE LA CÉDULA NO EXISTA ANTES DE GUARDAR
            var cedulaExiste = await _context.Empleados
                .AnyAsync(e => e.Cedula == empleadoDto.Cedula);

            if (cedulaExiste)
            {
                return BadRequest(new
                {
                    message = $"La cédula {empleadoDto.Cedula} ya está registrada en el sistema."
                });
            }

            // Crear el nuevo empleado
            var nuevoEmpleado = new Empleado
            {
                Cedula = empleadoDto.Cedula,
                Nombre = empleadoDto.Nombre,
                Apellido = empleadoDto.Apellido,
                Password = empleadoDto.Password, // ⚠️ Considera hashear en producción
                Rol = empleadoDto.Rol,
                CentroMedicoId = empleadoDto.CentroMedicoId
            };

            _context.Empleados.Add(nuevoEmpleado);
            await _context.SaveChangesAsync();

            // Cargar la relación para devolver el DTO completo
            await _context.Entry(nuevoEmpleado).Reference(e => e.CentroMedico).LoadAsync();

            var resultadoDto = new EmpleadoDto
            {
                Id = nuevoEmpleado.Id,
                Cedula = nuevoEmpleado.Cedula,
                Nombre = nuevoEmpleado.Nombre,
                Apellido = nuevoEmpleado.Apellido,
                Rol = nuevoEmpleado.Rol,
                CentroMedicoId = nuevoEmpleado.CentroMedicoId,
                NombreCentroMedico = nuevoEmpleado.CentroMedico != null ? nuevoEmpleado.CentroMedico.Nombre : "Sin Asignar"
            };

            return CreatedAtAction(nameof(GetEmpleado), new { id = resultadoDto.Id }, resultadoDto);
        }
        catch (DbUpdateException ex)
        {
            // Capturar error de duplicado de cédula a nivel de base de datos
            if (ex.InnerException?.Message.Contains("Duplicate entry") == true)
            {
                return BadRequest(new
                {
                    message = $"La cédula {empleadoDto.Cedula} ya está registrada en el sistema."
                });
            }
            return StatusCode(500, new { message = "Error al crear empleado", error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear empleado", error = ex.Message });
        }
    }

    // PUT: api/Empleados/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEmpleado(int id, EmpleadoCreateDto empleadoDto)
    {
        try
        {
            var empleado = await _context.Empleados.FindAsync(id);

            if (empleado == null)
            {
                return NotFound();
            }

            // ✅ VALIDAR que la cédula no esté en uso por OTRO empleado
            var cedulaEnUso = await _context.Empleados
                .AnyAsync(e => e.Cedula == empleadoDto.Cedula && e.Id != id);

            if (cedulaEnUso)
            {
                return BadRequest(new
                {
                    message = $"La cédula {empleadoDto.Cedula} ya está registrada por otro empleado."
                });
            }

            // Actualizar datos
            empleado.Cedula = empleadoDto.Cedula;
            empleado.Nombre = empleadoDto.Nombre;
            empleado.Apellido = empleadoDto.Apellido;
            empleado.Rol = empleadoDto.Rol;
            empleado.CentroMedicoId = empleadoDto.CentroMedicoId;

            // Actualizar contraseña solo si se proporciona
            if (!string.IsNullOrEmpty(empleadoDto.Password))
            {
                empleado.Password = empleadoDto.Password; // ⚠️ Considera hashear en producción
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("Duplicate entry") == true)
            {
                return BadRequest(new
                {
                    message = $"La cédula {empleadoDto.Cedula} ya está registrada por otro empleado."
                });
            }
            return StatusCode(500, new { message = "Error al actualizar empleado", error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar empleado", error = ex.Message });
        }
    }

    // DELETE: api/Empleados/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmpleado(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
        {
            return NotFound();
        }

        _context.Empleados.Remove(empleado);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}