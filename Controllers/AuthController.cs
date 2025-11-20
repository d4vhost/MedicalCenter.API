// Archivo: Controllers/AuthController.cs
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

        // --- LOGIN EMPLEADOS (Se mantiene igual) ---
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            var empleado = await _globalContext.Empleados
                .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

            if (empleado == null || empleado.Password != loginRequest.Password)
                return Unauthorized(new { message = "Cédula o contraseña incorrecta." });

            if (string.IsNullOrEmpty(empleado.Rol) || empleado.CentroMedicoId == null)
                return Unauthorized(new { message = "Cuenta no configurada correctamente." });

            var token = GenerateJwtToken(empleado);

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

        // --- LOGIN PACIENTES (NUEVO Y LIMPIO) ---
        [AllowAnonymous]
        [HttpPost("login-paciente")]
        public async Task<IActionResult> LoginPaciente([FromBody] PacienteLoginRequestDto request)
        {
            // 1. Buscar en Global
            var paciente = await _globalContext.Pacientes
                .FirstOrDefaultAsync(p => p.Cedula == request.Cedula);

            if (paciente == null) return NotFound(new { message = "Cédula no encontrada." });

            // 2. Validar Fecha
            if (paciente.FechaNacimiento?.Date != request.FechaNacimiento.Date)
                return Unauthorized(new { message = "Fecha de nacimiento incorrecta." });

            // 3. Token SIN Roles y SIN Centro Médico
            var token = GenerateJwtTokenPaciente(paciente);

            return Ok(new
            {
                token = token,
                empleadoId = paciente.Id,
                nombreCompleto = $"{paciente.Nombre} {paciente.Apellido}"
            });
        }

        // ... (CheckCedula y GenerateJwtToken de empleado igual) ...
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

        private string GenerateJwtToken(Empleado empleado)
        {
            var jwtKey = _configuration["JWT:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.GivenName, empleado.Nombre),
                new Claim(ClaimTypes.Surname, empleado.Apellido),
                new Claim(ClaimTypes.Role, empleado.Rol!.Trim()),
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

        // --- TOKEN PACIENTE (SIN "TRUCOS") ---
        private string GenerateJwtTokenPaciente(Paciente paciente)
        {
            var jwtKey = _configuration["JWT:Key"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                // Solo la identidad del paciente
                new Claim(ClaimTypes.NameIdentifier, paciente.Id.ToString()),
                new Claim(ClaimTypes.GivenName, paciente.Nombre),
                new Claim(ClaimTypes.Surname, paciente.Apellido)
                // ¡SIN ROL Y SIN CENTRO ID!
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