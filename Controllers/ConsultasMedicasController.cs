// Archivo: Controllers/ConsultasMedicasController.cs

using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // <-- ¡¡IMPORTANTE!!

namespace MedicalCenter.API.Controllers
{
    [Authorize] // Requiere estar logueado
    [Route("api/[controller]")]
    [ApiController]
    public class ConsultasMedicasController : ControllerBase
    {
        // CAMBIO: Inyectar la FÁBRICA de contextos locales
        private readonly ILocalDbContextFactory _localContextFactory;
        // CAMBIO: Inyectar el contexto GLOBAL (para validar Pacientes y Médicos)
        private readonly GlobalDbContext _globalContext;

        public ConsultasMedicasController(ILocalDbContextFactory localContextFactory, GlobalDbContext globalContext)
        {
            _localContextFactory = localContextFactory;
            _globalContext = globalContext;
        }

        // --- Helper para obtener el contexto local correcto ---
        private LocalDbContext GetContextFromToken()
        {
            // 1. Buscar el claim "centro_medico_id" que añadimos en el AuthController
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                throw new InvalidOperationException("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // 2. La fábrica crea el DbContext para Guayaquil (ID 2) o Cuenca (ID 3)
            return _localContextFactory.CreateDbContext(centroId);
        }
        // --- Fin del Helper ---

        // GET: api/ConsultasMedicas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultaMedica>>> GetConsultasMedicas()
        {
            // 'using' asegura que la conexión se cierre al terminar
            using (var _context = GetContextFromToken())
            {
                // Devuelve SÓLO las consultas del centro médico del usuario (Guayaquil o Cuenca)
                return await _context.ConsultasMedicas
                                     .OrderByDescending(c => c.FechaHora)
                                     .ToListAsync();
            }
        }

        // GET: api/ConsultasMedicas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ConsultaMedica>> GetConsultaMedica(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var consultaMedica = await _context.ConsultasMedicas.FindAsync(id);

                if (consultaMedica == null)
                {
                    return NotFound("Consulta no encontrada en este centro médico.");
                }

                return consultaMedica;
            }
        }

        // POST: api/ConsultasMedicas
        [HttpPost]
        public async Task<ActionResult<ConsultaMedica>> PostConsultaMedica(ConsultaMedica consultaMedica)
        {
            // 1. Validar que las claves foráneas (Paciente, Medico) existan en la DB GLOBAL
            var pacienteExiste = await _globalContext.Pacientes.AnyAsync(p => p.Id == consultaMedica.PacienteId);
            var medicoExiste = await _globalContext.Medicos.AnyAsync(m => m.Id == consultaMedica.MedicoId);

            if (!pacienteExiste || !medicoExiste)
            {
                return BadRequest(new { message = "El PacienteId o MedicoId no existen en la base de datos global." });
            }

            // 2. Obtener el contexto local y guardar la consulta
            using (var _context = GetContextFromToken())
            {
                // Asignar la fecha y hora actual si no viene
                if (consultaMedica.FechaHora == default)
                {
                    consultaMedica.FechaHora = DateTime.UtcNow;
                }

                _context.ConsultasMedicas.Add(consultaMedica);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetConsultaMedica", new { id = consultaMedica.Id }, consultaMedica);
            }
        }

        // PUT: api/ConsultasMedicas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConsultaMedica(int id, ConsultaMedica consultaMedica)
        {
            if (id != consultaMedica.Id)
            {
                return BadRequest();
            }

            using (var _context = GetContextFromToken())
            {
                // Validar que la consulta exista en este contexto
                var existe = await _context.ConsultasMedicas.AnyAsync(e => e.Id == id);
                if (!existe)
                {
                    return NotFound("La consulta no existe en este centro médico.");
                }

                _context.Entry(consultaMedica).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // DELETE: api/ConsultasMedicas/5
        [Authorize(Roles = "Admin")] // Solo admin puede borrar consultas
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultaMedica(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var consultaMedica = await _context.ConsultasMedicas.FindAsync(id);
                if (consultaMedica == null)
                {
                    return NotFound();
                }

                _context.ConsultasMedicas.Remove(consultaMedica);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }
    }
}