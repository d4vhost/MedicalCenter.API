// Archivo: Controllers/EmpleadosController.cs

using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.API.Controllers
{
    [Authorize(Roles = "ADMINISTRATIVO")] // <-- SOLO ADMIN PUEDE GESTIONAR EMPLEADOS
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        // CAMBIO: Inyectar GlobalDbContext
        private readonly GlobalDbContext _context;

        public EmpleadosController(GlobalDbContext context)
        {
            _context = context;
        }

        // GET: api/Empleados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados()
        {
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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpleado(int id, Empleado empleado)
        {
            if (id != empleado.Id)
            {
                return BadRequest();
            }

            // 1. BUSCAR EL EMPLEADO QUE YA EXISTE EN LA BD
            var empleadoExistente = await _context.Empleados.FindAsync(id);

            if (empleadoExistente == null)
            {
                return NotFound(); // Esto es lo que te está pasando ahora (no encuentra el ID)
            }

            // 2. ACTUALIZAR SOLO LOS CAMPOS DE DATOS (Manual)
            empleadoExistente.Cedula = empleado.Cedula;
            empleadoExistente.Nombre = empleado.Nombre;
            empleadoExistente.Apellido = empleado.Apellido;
            empleadoExistente.Rol = empleado.Rol;
            empleadoExistente.CentroMedicoId = empleado.CentroMedicoId;

            // 3. VALIDACIÓN DE SEGURIDAD PARA LA CONTRASEÑA
            // Solo la actualizamos si el usuario envió una nueva (no vacía)
            if (!string.IsNullOrEmpty(empleado.Password))
            {
                // Aquí podrías hashearla en el futuro
                empleadoExistente.Password = empleado.Password;
            }
            // Si viene vacía, NO tocamos la 'empleadoExistente.Password', conservando la anterior.

            try
            {
                // Guardamos los cambios
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
        [HttpPost]
        public async Task<ActionResult<Empleado>> PostEmpleado(Empleado empleado)
        {
            // NOTA: Aquí deberías "hashear" la contraseña antes de guardarla
            // empleado.Password = Hash(empleado.Password);

            _context.Empleados.Add(empleado);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmpleado", new { id = empleado.Id }, empleado);
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