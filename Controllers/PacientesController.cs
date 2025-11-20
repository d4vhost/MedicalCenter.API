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
        // 1. LISTADO DE PACIENTES
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
        // 2. HISTORIAL (CORREGIDO PARA BUSCAR EN TODOS LOS NODOS)
        // ==========================================
        [Authorize]
        [HttpGet("{id}/historial")]
        public async Task<ActionResult<object>> GetHistorialPaciente(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Validaciones de Seguridad
            bool esPersonalSalud = userRole == "ADMINISTRATIVO" || userRole == "MEDICO";
            bool esElPropioPaciente = userRole == "PACIENTE" && userId == id.ToString();

            if (!esPersonalSalud && !esElPropioPaciente)
            {
                return Forbid();
            }

            // 2. Validar que el paciente exista en la Global
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound(new { message = "Paciente no encontrado." });

            // --- LISTAS PARA ACUMULAR DATOS DE TODOS LOS NODOS ---
            var todasConsultas = new List<object>();
            var todosDiagnosticos = new List<object>();
            var todasPrescripciones = new List<object>();

            // 3. DEFINIR QUÉ NODOS VAMOS A CONSULTAR
            // Si es un médico, quizás solo quieras ver su centro. 
            // Pero si es el PACIENTE, él quiere ver TODO su historial sin importar el hospital.
            // Aquí definimos los IDs de tus nodos configurados en LocalDbContextFactory (2: Guayaquil, 3: Cuenca)
            List<int> nodosAconsultar = new List<int> { 2, 3 };

            // 4. RECORRER CADA NODO Y EXTRAER INFORMACIÓN
            foreach (var nodoId in nodosAconsultar)
            {
                try
                {
                    using (var localContext = _localContextFactory.CreateDbContext(nodoId))
                    {
                        // A. Buscar consultas en este nodo
                        var consultas = await localContext.ConsultasMedicas
                            .Where(c => c.PacienteId == id)
                            .OrderByDescending(c => c.FechaHora)
                            .Select(c => new { c.Id, c.FechaHora, c.PacienteId, c.MedicoId, c.Motivo, NodoOrigen = nodoId }) // Agregamos NodoOrigen para saber de dónde vino
                            .ToListAsync();

                        if (consultas.Any())
                        {
                            // Agregamos las consultas encontradas a la lista general
                            todasConsultas.AddRange(consultas);

                            // B. Buscar Diagnósticos vinculados a estas consultas
                            var consultaIds = consultas.Select(c => c.Id).ToList();
                            var diagnosticosData = await localContext.Diagnosticos
                                .Where(d => consultaIds.Contains(d.ConsultaId))
                                .ToListAsync();

                            var diagnosticosResult = diagnosticosData.Select(d => new
                            {
                                d.Id,
                                d.ConsultaId,
                                d.EnfermedadNombre,
                                d.Observaciones,
                                NodoOrigen = nodoId
                            }).ToList();
                            todosDiagnosticos.AddRange(diagnosticosResult);

                            // C. Buscar Prescripciones vinculadas a estos diagnósticos
                            var diagnosticoIds = diagnosticosData.Select(d => d.Id).ToList();
                            var prescripciones = await localContext.Prescripciones
                                .Where(p => diagnosticoIds.Contains(p.DiagnosticoId))
                                .ToListAsync();

                            // Para los nombres de medicamentos, consultamos la DB Global (solo IDs nuevos)
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
                                p.Indicaciones,
                                NodoOrigen = nodoId
                            }).ToList();
                            todasPrescripciones.AddRange(prescripcionesDto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si un nodo está apagado (ej. se cayó el server de Cuenca), 
                    // capturamos el error para seguir mostrando la info de los otros nodos.
                    Console.WriteLine($"Error conectando al nodo {nodoId}: {ex.Message}");
                }
            }

            // 5. RETORNAR LA INFORMACIÓN CONSOLIDADA
            return Ok(new
            {
                consultas = todasConsultas.OrderByDescending(x => ((dynamic)x).FechaHora), // Reordenar por fecha mezclada
                diagnosticos = todosDiagnosticos,
                prescripciones = todasPrescripciones
            });
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