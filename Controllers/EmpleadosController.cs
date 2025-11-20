// Archivo: Controllers/EmpleadosController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs; // <--- ¡IMPORTANTE! Para reconocer EmpleadoUpdateDto y EmpleadoCreateDto
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Controllers
{
    [Authorize(Roles = "ADMINISTRATIVO")]
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        private readonly GlobalDbContext _context;

        public EmpleadosController(GlobalDbContext context)
        {
            _context = context;
        }

        // GET: api/Empleados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados()
        {
            // Retornamos la lista completa de la entidad (esto no afecta tu error actual)
            return await _context.Empleados.ToListAsync();
        }

        // GET: api/Empleados/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Empleado>> GetEmpleado(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);

            if (empleado == null)
            {
                return NotFound();
            }

            return empleado;
        }

        // PUT: api/Empleados/5
        // ✅ CAMBIO CLAVE: Recibimos 'EmpleadoUpdateDto' en lugar de 'Empleado'
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpleado(int id, EmpleadoUpdateDto empleadoDto)
        {
            if (id != empleadoDto.Id)
            {
                return BadRequest("El ID de la URL no coincide con el ID del cuerpo de la solicitud.");
            }

            // 1. BUSCAR EL EMPLEADO EXISTENTE EN LA BD (Para editarlo)
            var empleadoExistente = await _context.Empleados.FindAsync(id);

            if (empleadoExistente == null)
            {
                return NotFound();
            }

            // 2. ACTUALIZAR CAMPOS DE DATOS MANUALMENTE
            // Pasamos los datos del DTO a la Entidad de base de datos
            empleadoExistente.Cedula = empleadoDto.Cedula;
            empleadoExistente.Nombre = empleadoDto.Nombre;
            empleadoExistente.Apellido = empleadoDto.Apellido;
            empleadoExistente.Rol = empleadoDto.Rol;
            empleadoExistente.CentroMedicoId = empleadoDto.CentroMedicoId;

            // 3. ACTUALIZAR CONTRASEÑA SOLO SI SE ENVÍA UNA NUEVA
            // Como en el UpdateDto la password es opcional (string?), esto no fallará si viene vacía.
            if (!string.IsNullOrEmpty(empleadoDto.Password))
            {
                // Aquí podrías agregar el hasheo si tuvieras el método
                empleadoExistente.Password = empleadoDto.Password;
            }
            // Si empleadoDto.Password es null o vacío, simplemente NO tocamos empleadoExistente.Password
            // manteniendo la contraseña vieja.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Empleados.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Empleados
        // ✅ CAMBIO: Usamos 'EmpleadoCreateDto' para la creación
        [HttpPost]
        public async Task<ActionResult<Empleado>> PostEmpleado(EmpleadoCreateDto empleadoDto)
        {
            // 1. Convertir el DTO a la Entidad Empleado
            var nuevoEmpleado = new Empleado
            {
                Cedula = empleadoDto.Cedula,
                Nombre = empleadoDto.Nombre,
                Apellido = empleadoDto.Apellido,
                // En el CreateDto, el Password SÍ es obligatorio, así que lo asignamos directo
                Password = empleadoDto.Password,
                Rol = empleadoDto.Rol,
                CentroMedicoId = empleadoDto.CentroMedicoId
            };

            _context.Empleados.Add(nuevoEmpleado);
            await _context.SaveChangesAsync();

            // Retornamos el empleado creado
            return CreatedAtAction("GetEmpleado", new { id = nuevoEmpleado.Id }, nuevoEmpleado);
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
}