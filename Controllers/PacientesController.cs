using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
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

        // Método auxiliar para obtener el centro del token
        private int? GetCentroIdFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                return null;
            }
            return centroId;
        }

        // ==========================================
        // 1. LISTADO DE PACIENTES (CORREGIDO)
        // ==========================================
        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PacienteDto>>> GetPacientes()
        {
            // 1. Obtener datos globales
            var pacientes = await _context.Pacientes.ToListAsync();
            var medicos = await _context.Empleados.Where(e => e.Rol == "MEDICO").ToListAsync();
            var centros = await _context.CentrosMedicos.ToListAsync();

            // 2. Obtener datos locales
            int nodoPorDefecto = 2;
            using var localContext = _localContextFactory.CreateDbContext(nodoPorDefecto);

            var consultas = await localContext.ConsultasMedicas.ToListAsync();

            // Traemos solo los diagnósticos que correspondan a las consultas encontradas
            var consultaIds = consultas.Select(c => c.Id).ToList();
            var diagnosticos = await localContext.Diagnosticos
                                    .Where(d => consultaIds.Contains(d.ConsultaId))
                                    .ToListAsync();

            // 3. Mapear y cruzar información
            var pacientesDto = pacientes.Select(p =>
            {
                // Buscar la última consulta de este paciente
                var ultimaConsulta = consultas
                    .Where(c => c.PacienteId == p.Id)
                    .OrderByDescending(c => c.FechaHora)
                    .FirstOrDefault();

                string estadoCalculado = "Sin Consultas";
                string centroMedicoNombre = "No Asignado";

                if (ultimaConsulta != null)
                {
                    // Lógica para el ESTADO: Buscamos si existe algún diagnóstico para esta consulta
                    bool tieneDiagnostico = diagnosticos.Any(d => d.ConsultaId == ultimaConsulta.Id);

                    if (tieneDiagnostico)
                    {
                        estadoCalculado = "Finalizado";
                    }
                    else
                    {
                        estadoCalculado = "En Proceso";
                    }

                    // Lógica para el CENTRO MÉDICO
                    var medicoQueAtendio = medicos.FirstOrDefault(m => m.Id == ultimaConsulta.MedicoId);
                    if (medicoQueAtendio != null)
                    {
                        var centro = centros.FirstOrDefault(c => c.Id == medicoQueAtendio.CentroMedicoId);
                        if (centro != null)
                        {
                            centroMedicoNombre = centro.Nombre;
                        }
                    }
                }

                return new PacienteDto
                {
                    Id = p.Id,
                    Cedula = p.Cedula,
                    Nombre = p.Nombre,
                    Apellido = p.Apellido,
                    FechaNacimiento = p.FechaNacimiento, // ✅ Ahora acepta nullable
                    Direccion = p.Direccion,
                    Estado = estadoCalculado,
                    CentroMedico = centroMedicoNombre
                };
            }).ToList();

            return Ok(pacientesDto);
        }

        // ==========================================
        // 2. HISTORIAL
        // ==========================================
        [Authorize]
        [HttpGet("{id}/historial")]
        public async Task<ActionResult<object>> GetHistorialPaciente(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                if (userId == null || userId != id.ToString())
                    return Forbid();
            }
            else
            {
                if (userRole != "ADMINISTRATIVO" && userRole != "MEDICO")
                    return Forbid();
            }

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound(new { message = "Paciente no encontrado." });

            var centroIdToken = GetCentroIdFromToken();
            int centroTarget = centroIdToken.HasValue ? centroIdToken.Value : 2;

            using (var localContext = _localContextFactory.CreateDbContext(centroTarget))
            {
                var consultas = await localContext.ConsultasMedicas
                    .Where(c => c.PacienteId == id)
                    .OrderByDescending(c => c.FechaHora)
                    .Select(c => new { c.Id, c.FechaHora, c.PacienteId, c.MedicoId, c.Motivo })
                    .ToListAsync();

                if (!consultas.Any())
                {
                    return Ok(new { consultas = new List<object>(), diagnosticos = new List<object>(), prescripciones = new List<object>() });
                }

                var consultaIds = consultas.Select(c => c.Id).ToList();
                var diagnosticosData = await localContext.Diagnosticos
                    .Where(d => consultaIds.Contains(d.ConsultaId))
                    .ToListAsync();

                var diagnosticosResult = diagnosticosData.Select(d => new
                {
                    d.Id,
                    d.ConsultaId,
                    d.EnfermedadNombre,
                    d.Observaciones
                }).ToList();

                var diagnosticoIds = diagnosticosData.Select(d => d.Id).ToList();
                var prescripciones = await localContext.Prescripciones
                    .Where(p => diagnosticoIds.Contains(p.DiagnosticoId))
                    .ToListAsync();

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

        // ==========================================
        // 3. OTROS MÉTODOS CRUD
        // ==========================================

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpGet("{id}")]
        public async Task<ActionResult<PacienteDto>> GetPaciente(int id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound();

            return new PacienteDto
            {
                Id = paciente.Id,
                Cedula = paciente.Cedula,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                FechaNacimiento = paciente.FechaNacimiento, // ✅ Ahora acepta nullable
                Direccion = paciente.Direccion
            };
        }

        [Authorize(Roles = "ADMINISTRATIVO, MEDICO")]
        [HttpPost]
        public async Task<ActionResult<PacienteDto>> PostPaciente(PacienteCreateDto pacienteDto)
        {
            if (await _context.Pacientes.AnyAsync(p => p.Cedula == pacienteDto.Cedula))
            {
                return BadRequest("Ya existe un paciente con esa cédula.");
            }

            // ✅ Validación de fecha de nacimiento
            if (!pacienteDto.FechaNacimiento.HasValue)
            {
                return BadRequest("La fecha de nacimiento es obligatoria.");
            }

            var paciente = new Paciente
            {
                Cedula = pacienteDto.Cedula,
                Nombre = pacienteDto.Nombre.ToUpper(),
                Apellido = pacienteDto.Apellido.ToUpper(),
                FechaNacimiento = pacienteDto.FechaNacimiento.Value, // ✅ Extrae el valor
                Direccion = pacienteDto.Direccion?.ToUpper()
            };

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            var newPacienteDto = new PacienteDto
            {
                Id = paciente.Id,
                Cedula = paciente.Cedula,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                FechaNacimiento = paciente.FechaNacimiento, // ✅ Ahora es compatible
                Direccion = paciente.Direccion
            };

            return CreatedAtAction("GetPaciente", new { id = paciente.Id }, newPacienteDto);
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