// Archivo: Controllers/PacientesController.cs
using MedicalCenter.API.Data;
using MedicalCenter.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacientesController : ControllerBase
    {
        private readonly GlobalDbContext _context;
        private readonly ILocalDbContextFactory _localContextFactory;

        public PacientesController(GlobalDbContext context, ILocalDbContextFactory localContextFactory)
        {
            _context = context;
            _localContextFactory = localContextFactory;
        }

        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                return null;
            }
            return centroId;
        }

        // GET: api/Pacientes/1/historial
        [Authorize] // 🔓 Abierto a cualquier usuario autenticado (con o sin rol)
        [HttpGet("{id}/historial")]
        public async Task<ActionResult<object>> GetHistorialPaciente(int id)
        {
            // --- 1. VALIDACIÓN DE SEGURIDAD ---
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Si NO tiene rol (es Paciente), verificamos que solo consulte su propia data
            if (string.IsNullOrEmpty(userRole))
            {
                if (userId == null || userId != id.ToString())
                    return Forbid(); // ⛔ No puede ver historial ajeno
            }
            else
            {
                // Si tiene rol, verificamos que sea personal autorizado
                if (userRole != "ADMINISTRATIVO" && userRole != "MEDICO")
                    return Forbid();
            }

            // --- 2. VALIDAR EXISTENCIA DEL PACIENTE ---
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound(new { message = "Paciente no encontrado." });

            // --- 3. DETERMINAR DÓNDE BUSCAR LOS DATOS ---
            var centroIdToken = GetCentroIdFromToken();

            // Lista de centros a consultar. 
            // Si es médico/admin, usamos SU centro.
            // Si es paciente (token sin centro), usamos los centros por defecto (ej. ID 2 y 3)
            var centrosAConsultar = new List<int>();

            if (centroIdToken.HasValue)
            {
                centrosAConsultar.Add(centroIdToken.Value);
            }
            else
            {
                // Lógica para Paciente: Consultar bases locales disponibles.
                // En tu caso, tus datos están en el nodo 2 (Guayaquil).
                centrosAConsultar.Add(2);
                // Si tuvieras más nodos activos, podrías agregarlos aquí: centrosAConsultar.Add(3);
            }

            // --- 4. CONSULTA Y CONSOLIDACIÓN ---
            // Usamos el primer centro disponible (para simplificar la respuesta JSON)
            // En un sistema real haríamos un 'foreach' y uniríamos las listas.
            if (!centrosAConsultar.Any()) return BadRequest("No se pudo determinar el centro de datos.");

            var centroTarget = centrosAConsultar.First();

            using (var localContext = _localContextFactory.CreateDbContext(centroTarget))
            {
                // A) Consultas (Proyección anónima)
                var consultas = await localContext.ConsultasMedicas
                    .Where(c => c.PacienteId == id)
                    .OrderByDescending(c => c.FechaHora)
                    .Select(c => new { c.Id, c.FechaHora, c.PacienteId, c.MedicoId, c.Motivo })
                    .ToListAsync();

                if (!consultas.Any())
                {
                    return Ok(new { consultas = new List<object>(), diagnosticos = new List<object>(), prescripciones = new List<object>() });
                }

                // B) Diagnósticos
                var consultaIds = consultas.Select(c => c.Id).ToList();
                var diagnosticosData = await localContext.Diagnosticos
                    .Where(d => consultaIds.Contains(d.ConsultaId))
                    .ToListAsync();

                // Proyección Diagnósticos
                var diagnosticosResult = diagnosticosData.Select(d => new
                {
                    d.Id,
                    d.ConsultaId,
                    d.EnfermedadNombre,
                    d.Observaciones
                }).ToList();

                // C) Prescripciones
                var diagnosticoIds = diagnosticosData.Select(d => d.Id).ToList();
                var prescripciones = await localContext.Prescripciones
                    .Where(p => diagnosticoIds.Contains(p.DiagnosticoId))
                    .ToListAsync();

                // D) Nombres de Medicamentos (Global)
                var medicamentoIds = prescripciones.Select(p => p.MedicamentoId).Distinct().ToList();
                var medicamentosInfo = await _context.Medicamentos
                    .Where(m => medicamentoIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, m => m.NombreGenerico);

                var prescripcionesDto = prescripciones.Select(p => new
                {
                    p.Id,
                    p.DiagnosticoId,
                    p.MedicamentoId,
                    NombreMedicamento = medicamentosInfo.ContainsKey(p.MedicamentoId) ? medicamentosInfo[p.MedicamentoId] : "DESCONOCIDO",
                    p.Indicaciones
                }).ToList();

                return Ok(new
                {
                    consultas,
                    diagnosticos = diagnosticosResult,
                    prescripciones = prescripcionesDto
                });
            }
        }

        // --- LOS DEMÁS MÉTODOS SIGUEN PROTEGIDOS IGUAL ---

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paciente>>> GetPacientes() => await _context.Pacientes.ToListAsync();

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Paciente>> GetPaciente(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            return paciente != null ? paciente : NotFound();
        }

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaciente(int id, Paciente paciente)
        {
            if (id != paciente.Id) return BadRequest();
            _context.Entry(paciente).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPost]
        public async Task<ActionResult<Paciente>> PostPaciente(Paciente paciente)
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetPaciente", new { id = paciente.Id }, paciente);
        }

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("existe/{cedula}")]
        public async Task<ActionResult<object>> GetPacientePorCedula(string cedula)
        {
            var paciente = await _context.Pacientes
               .Where(p => p.Cedula == cedula)
               .Select(p => new { p.Id })
               .FirstOrDefaultAsync();
            return Ok(paciente);
        }

        [Authorize(Roles = "ADMINISTRATIVO")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaciente(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound();
            _context.Pacientes.Remove(paciente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}