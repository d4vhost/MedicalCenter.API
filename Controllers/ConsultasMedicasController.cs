// Archivo: Controllers/ConsultasMedicasController.cs

using MedicalCenter.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // <-- ¡¡IMPORTANTE!!
using System.Collections.Generic; // <-- Para new List<>()

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

        // --- INICIO DE HELPERS CORREGIDOS ---

        // Helper para OBTENER el ID del centro desde el token
        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                // No se pudo encontrar el claim, o no es un número
                return null;
            }
            return centroId;
        }

        // Helper para CREAR el contexto basado en un ID
        private LocalDbContext GetContextFromToken(int centroId)
        {
            // La fábrica crea el DbContext para Guayaquil (ID 2) o Cuenca (ID 3)
            return _localContextFactory.CreateDbContext(centroId);
        }
        // --- FIN DE HELPERS CORREGIDOS ---


        // GET: api/ConsultasMedicas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultaMedica>>> GetConsultasMedicas()
        {
            var centroId = GetCentroIdFromToken();

            if (!centroId.HasValue)
            {
                return Unauthorized("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // --- ¡SOLUCIÓN! ---
            // Si el usuario es de Quito (ID 1), es el admin global.
            // No tiene DB local, así que devolvemos una lista vacía para evitar el error 500.
            if (centroId.Value == 1)
            {
                return Ok(new List<ConsultaMedica>());
            }
            // --- Fin de la solución ---

            // 'using' asegura que la conexión se cierre al terminar
            using (var _context = GetContextFromToken(centroId.Value))
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
            var centroId = GetCentroIdFromToken();

            if (!centroId.HasValue)
            {
                return Unauthorized("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // --- ¡SOLUCIÓN! ---
            // Si es Admin Global, no puede obtener consultas locales por ID.
            if (centroId.Value == 1)
            {
                return NotFound("Consulta no encontrada en este centro médico.");
            }
            // --- Fin de la solución ---

            using (var _context = GetContextFromToken(centroId.Value))
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
            var centroId = GetCentroIdFromToken();

            if (!centroId.HasValue)
            {
                return Unauthorized("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // --- ¡SOLUCIÓN! ---
            // El Admin Global (ID 1) no puede CREAR consultas locales.
            if (centroId.Value == 1)
            {
                return Forbid("El administrador global no puede crear consultas locales.");
            }
            // --- Fin de la solución ---

            // 1. Validar que las claves foráneas (Paciente, Medico) existan en la DB GLOBAL
            var pacienteExiste = await _globalContext.Pacientes.AnyAsync(p => p.Id == consultaMedica.PacienteId);
            var medicoExiste = await _globalContext.Medicos.AnyAsync(m => m.Id == consultaMedica.MedicoId);

            if (!pacienteExiste || !medicoExiste)
            {
                return BadRequest(new { message = "El PacienteId o MedicoId no existen en la base de datos global." });
            }

            // 2. Obtener el contexto local y guardar la consulta
            using (var _context = GetContextFromToken(centroId.Value))
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

            var centroId = GetCentroIdFromToken();

            if (!centroId.HasValue)
            {
                return Unauthorized("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // --- ¡SOLUCIÓN! ---
            // El Admin Global (ID 1) no puede MODIFICAR consultas locales.
            if (centroId.Value == 1)
            {
                return Forbid("El administrador global no puede modificar consultas locales.");
            }
            // --- Fin de la solución ---

            using (var _context = GetContextFromToken(centroId.Value))
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
        [Authorize(Roles = "ADMINISTRATIVO")] // Solo admin puede borrar consultas
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConsultaMedica(int id)
        {
            var centroId = GetCentroIdFromToken();

            if (!centroId.HasValue)
            {
                return Unauthorized("Token de usuario no contiene un 'centro_medico_id' válido.");
            }

            // --- ¡SOLUCIÓN! ---
            // El Admin Global (ID 1) no puede BORRAR consultas locales...
            // Omitimos esto porque ya está protegido por [Authorize(Roles = "ADMINISTRATIVO")]
            // y el único admin (David) tiene ID 1. Si tuvieras admins locales,
            // necesitarías la comprobación `if (centroId.Value == 1) return Forbid();`

            // ...PERO, SÍ NECESITAMOS EVITAR EL ERROR 500
            if (centroId.Value == 1)
            {
                return Forbid("El administrador global no puede eliminar consultas locales.");
            }
            // --- Fin de la solución ---

            using (var _context = GetContextFromToken(centroId.Value))
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