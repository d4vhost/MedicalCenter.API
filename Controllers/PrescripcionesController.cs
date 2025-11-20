// Archivo: Controllers/PrescripcionesController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs; // Importante para el DTO
using MedicalCenter.API.Models.Entities; // O donde tengas tus entidades
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalCenter.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PrescripcionesController : ControllerBase
    {
        private readonly ILocalDbContextFactory _localContextFactory;
        private readonly GlobalDbContext _globalContext;

        public PrescripcionesController(ILocalDbContextFactory localContextFactory, GlobalDbContext globalContext)
        {
            _localContextFactory = localContextFactory;
            _globalContext = globalContext;
        }

        // --- Helper para obtener el contexto local correcto ---
        private LocalDbContext GetContextFromToken()
        {
            var centroIdClaim = User.FindFirst("centro_medico_id");
            if (centroIdClaim == null || !int.TryParse(centroIdClaim.Value, out var centroId))
            {
                throw new InvalidOperationException("Token de usuario no contiene un 'centro_medico_id' válido.");
            }
            return _localContextFactory.CreateDbContext(centroId);
        }

        // GET: api/Prescripciones/PorDiagnostico/5
        // CORRECCIÓN: Ahora devolvemos PrescripcionDto con el nombre del medicamento
        [HttpGet("PorDiagnostico/{diagnosticoId}")]
        public async Task<ActionResult<IEnumerable<PrescripcionDto>>> GetPrescripcionesPorDiagnostico(int diagnosticoId)
        {
            using (var _context = GetContextFromToken())
            {
                // 1. Obtener las prescripciones de la BD LOCAL
                var prescripcionesLocal = await _context.Prescripciones
                    .Where(p => p.DiagnosticoId == diagnosticoId)
                    .ToListAsync();

                if (!prescripcionesLocal.Any())
                {
                    return new List<PrescripcionDto>();
                }

                // 2. Extraer los IDs de medicamentos necesarios
                var medicamentosIds = prescripcionesLocal.Select(p => p.MedicamentoId).Distinct().ToList();

                // 3. Consultar los nombres en la BD GLOBAL
                // Creamos un diccionario ID -> Nombre para búsqueda rápida
                var medicamentosInfo = await _globalContext.Medicamentos
                    .Where(m => medicamentosIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, m => m.NombreGenerico);

                // 4. Combinar todo en el DTO (El "Join" manual)
                var resultado = prescripcionesLocal.Select(p => new PrescripcionDto
                {
                    Id = p.Id,
                    DiagnosticoId = p.DiagnosticoId,
                    MedicamentoId = p.MedicamentoId,
                    Indicaciones = p.Indicaciones,
                    // Aquí asignamos el nombre buscando en el diccionario global
                    NombreMedicamento = medicamentosInfo.ContainsKey(p.MedicamentoId)
                                        ? medicamentosInfo[p.MedicamentoId]
                                        : "DESCONOCIDO"
                }).ToList();

                return resultado;
            }
        }

        // GET: api/Prescripciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Prescripcion>> GetPrescripcion(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var prescripcion = await _context.Prescripciones.FindAsync(id);
                if (prescripcion == null) return NotFound();
                return prescripcion;
            }
        }

        // POST: api/Prescripciones
        [HttpPost]
        public async Task<ActionResult<PrescripcionDto>> PostPrescripcion(PrescripcionCreateDto prescripcionDto)
        {
            // 1. Validar en GLOBAL
            var medicamento = await _globalContext.Medicamentos.FirstOrDefaultAsync(m => m.Id == prescripcionDto.MedicamentoId);
            if (medicamento == null)
            {
                return BadRequest(new { message = "El MedicamentoId no existe en la base de datos global." });
            }

            using (var _context = GetContextFromToken())
            {
                // 2. Validar en LOCAL
                var diagnosticoExiste = await _context.Diagnosticos.AnyAsync(d => d.Id == prescripcionDto.DiagnosticoId);
                if (!diagnosticoExiste)
                {
                    return BadRequest(new { message = "El DiagnosticoId no existe en este centro médico." });
                }

                // 3. Guardar
                var nuevaPrescripcion = new Prescripcion
                {
                    DiagnosticoId = prescripcionDto.DiagnosticoId,
                    MedicamentoId = prescripcionDto.MedicamentoId,
                    Indicaciones = prescripcionDto.Indicaciones
                };

                _context.Prescripciones.Add(nuevaPrescripcion);
                await _context.SaveChangesAsync();

                // 4. Retornar DTO completo (incluyendo el nombre que acabamos de validar)
                var resultDto = new PrescripcionDto
                {
                    Id = nuevaPrescripcion.Id,
                    DiagnosticoId = nuevaPrescripcion.DiagnosticoId,
                    MedicamentoId = nuevaPrescripcion.MedicamentoId,
                    Indicaciones = nuevaPrescripcion.Indicaciones,
                    NombreMedicamento = medicamento.NombreGenerico // Ya lo tenemos de la validación
                };

                return CreatedAtAction("GetPrescripcion", new { id = nuevaPrescripcion.Id }, resultDto);
            }
        }

        // DELETE: api/Prescripciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrescripcion(int id)
        {
            using (var _context = GetContextFromToken())
            {
                var prescripcion = await _context.Prescripciones.FindAsync(id);
                if (prescripcion == null) return NotFound();

                _context.Prescripciones.Remove(prescripcion);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        // PUT: api/Prescripciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPrescripcion(int id, Prescripcion prescripcion)
        {
            if (id != prescripcion.Id) return BadRequest();

            using (var _context = GetContextFromToken())
            {
                var existe = await _context.Prescripciones.AnyAsync(e => e.Id == id);
                if (!existe) return NotFound();

                _context.Entry(prescripcion).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }
    }
}