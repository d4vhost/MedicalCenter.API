using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace MedicalCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GlobalDbContext _globalContext;
        private readonly IConfiguration _configuration;

        public AuthController(GlobalDbContext globalContext, IConfiguration configuration)
        {
            _globalContext = globalContext;
            _configuration = configuration;
        }

        // --- LOGIN DE EMPLEADOS (MÉDICOS Y ADMINISTRATIVOS) ---
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            // 1. Buscar empleado por Cédula
            var empleado = await _globalContext.Empleados
                .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

            // 2. Validar existencia y contraseña (texto plano por ahora)
            if (empleado == null || empleado.Password != loginRequest.Password)
                return Unauthorized(new { message = "Cédula o contraseña incorrecta." });

            // 3. Validar que la cuenta esté configurada correctamente (Rol y Centro Médico obligatorios)
            // Gracias al arreglo en GlobalDbContext, estos datos ya no deberían llegar nulos.
            if (string.IsNullOrEmpty(empleado.Rol) || empleado.CentroMedicoId == null)
            {
                // Nota: Si ves este error, revisa el mapeo en GlobalDbContext.cs (snake_case vs PascalCase)
                return Unauthorized(new { message = "Cuenta no configurada correctamente (Falta Rol o Centro Médico)." });
            }

            // 4. Generar Token JWT
            var token = GenerateJwtToken(empleado);

            // 5. Responder con datos y token
            return Ok(new LoginResponseDto
            {
                Token = token,
                Id = empleado.Id,
                Nombre = empleado.Nombre,
                Apellido = empleado.Apellido,
                Rol = empleado.Rol.Trim(),
                CentroMedicoId = empleado.CentroMedicoId.Value
            });
        }

        // --- LOGIN DE PACIENTES ---
        [AllowAnonymous]
        [HttpPost("login-paciente")]
        public async Task<IActionResult> LoginPaciente([FromBody] PacienteLoginRequestDto request)
        {
            // 1. Buscar Paciente en DB Global
            var paciente = await _globalContext.Pacientes
                .FirstOrDefaultAsync(p => p.Cedula == request.Cedula);

            if (paciente == null)
                return NotFound(new { message = "Cédula no encontrada." });

            // 2. Validar Fecha de Nacimiento
            if (!paciente.FechaNacimiento.HasValue ||
                paciente.FechaNacimiento.Value.Date != request.FechaNacimiento.Date)
            {
                return Unauthorized(new { message = "Fecha de nacimiento incorrecta." });
            }

            // 3. Generar Token específico para Paciente
            var token = GenerateJwtTokenPaciente(paciente);

            return Ok(new
            {
                token = token,
                empleadoId = paciente.Id, // Usamos la misma propiedad en el frontend para el ID
                nombreCompleto = $"{paciente.Nombre} {paciente.Apellido}"
            });
        }

        // --- VERIFICAR CÉDULA (USADO EN REGISTRO/VALIDACIONES) ---
        [AllowAnonymous]
        [HttpGet("CheckCedula/{cedula}")]
        public async Task<IActionResult> CheckCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10)
                return BadRequest(new { message = "Cédula inválida" });

            var empleado = await _globalContext.Empleados
                .Where(e => e.Cedula == cedula)
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync();

            return (empleado != null) ? Ok(new { id = empleado.Id }) : NotFound();
        }

        // --- MÉTODOS PRIVADOS PARA GENERAR TOKENS ---

        private string GenerateJwtToken(Empleado empleado)
        {
            var jwtKey = _configuration["JWT:Key"];
            if (string.IsNullOrEmpty(jwtKey)) throw new Exception("Falta configuración JWT:Key");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.GivenName, empleado.Nombre),
                new Claim(ClaimTypes.Surname, empleado.Apellido),
                new Claim(ClaimTypes.Role, empleado.Rol!.Trim()), // Rol es seguro aquí por la validación previa
                new Claim("centro_medico_id", empleado.CentroMedicoId!.Value.ToString())
            };

            var token = new JwtSecurityToken(
                _configuration["JWT:Issuer"],
                _configuration["JWT:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateJwtTokenPaciente(Paciente paciente)
        {
            var jwtKey = _configuration["JWT:Key"];
            if (string.IsNullOrEmpty(jwtKey)) throw new Exception("Falta configuración JWT:Key");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, paciente.Id.ToString()),
                new Claim(ClaimTypes.GivenName, paciente.Nombre),
                new Claim(ClaimTypes.Surname, paciente.Apellido),
                new Claim(ClaimTypes.Role, "PACIENTE") // Rol explícito para control en frontend
            };

            var token = new JwtSecurityToken(
                _configuration["JWT:Issuer"],
                _configuration["JWT:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}