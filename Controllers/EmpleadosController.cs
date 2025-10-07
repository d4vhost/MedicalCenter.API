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
        // Usamos Include() para traer también la información del CentroMedico relacionado.
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
                NombreCentroMedico = e.CentroMedico.Nombre // Mapeamos el nombre
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
            NombreCentroMedico = empleado.CentroMedico.Nombre
        };

        return empleadoDto;
    }

    // POST: api/Empleados
    [HttpPost]
    public async Task<ActionResult<EmpleadoDto>> PostEmpleado(EmpleadoCreateDto empleadoDto)
    {
        var nuevoEmpleado = new Empleado
        {
            Cedula = empleadoDto.Cedula,
            Nombre = empleadoDto.Nombre,
            Apellido = empleadoDto.Apellido,
            Rol = empleadoDto.Rol,
            CentroMedicoId = empleadoDto.CentroMedicoId // Asignamos la clave foránea
        };

        _context.Empleados.Add(nuevoEmpleado);
        await _context.SaveChangesAsync();

        // Para devolver el DTO completo, necesitamos cargar el CentroMedico recién asignado
        await _context.Entry(nuevoEmpleado).Reference(e => e.CentroMedico).LoadAsync();

        var resultadoDto = new EmpleadoDto
        {
            Id = nuevoEmpleado.Id,
            Cedula = nuevoEmpleado.Cedula,
            Nombre = nuevoEmpleado.Nombre,
            Apellido = nuevoEmpleado.Apellido,
            Rol = nuevoEmpleado.Rol,
            CentroMedicoId = nuevoEmpleado.CentroMedicoId,
            NombreCentroMedico = nuevoEmpleado.CentroMedico.Nombre
        };

        return CreatedAtAction(nameof(GetEmpleado), new { id = resultadoDto.Id }, resultadoDto);
    }

    // PUT: api/Empleados/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEmpleado(int id, EmpleadoCreateDto empleadoDto)
    {
        var empleado = await _context.Empleados.FindAsync(id);

        if (empleado == null)
        {
            return NotFound();
        }

        empleado.Cedula = empleadoDto.Cedula;
        empleado.Nombre = empleadoDto.Nombre;
        empleado.Apellido = empleadoDto.Apellido;
        empleado.Rol = empleadoDto.Rol;
        empleado.CentroMedicoId = empleadoDto.CentroMedicoId;

        await _context.SaveChangesAsync();
        return NoContent();
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
