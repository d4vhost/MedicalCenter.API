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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            var empleado = await _globalContext.Empleados
                .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

            if (empleado == null || empleado.Password != loginRequest.Password)
            {
                return Unauthorized(new { message = "Cédula o contraseña incorrecta." });
            }

            if (string.IsNullOrEmpty(empleado.Rol) || empleado.CentroMedicoId == null)
            {
                return Unauthorized(new { message = "La cuenta del empleado no está configurada correctamente (falta Rol o Centro Médico)." });
            }

            var token = GenerateJwtToken(empleado);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Id = empleado.Id,
                Nombre = empleado.Nombre,
                Apellido = empleado.Apellido,
                // --- ✨ CORRECCIÓN 1 (Para el Frontend) ---
                Rol = empleado.Rol.Trim(),
                CentroMedicoId = empleado.CentroMedicoId.Value
            });
        }

        [AllowAnonymous]
        [HttpGet("CheckCedula/{cedula}")]
        public async Task<IActionResult> CheckCedula(string cedula)
        {
            // ... (Este método está bien)
            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10)
            {
                return BadRequest(new { message = "Cédula inválida" });
            }

            var empleado = await _globalContext.Empleados
                .Where(e => e.Cedula == cedula)
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync();

            if (empleado != null)
            {
                return Ok(new { id = empleado.Id });
            }

            return NotFound();
        }

        private string GenerateJwtToken(Empleado empleado)
        {
            var jwtKey = _configuration["JWT:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key no está configurada en appsettings.json");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.GivenName, empleado.Nombre),
                new Claim(ClaimTypes.Surname, empleado.Apellido),
                
                // --- ✨ CORRECCIÓN 2 (Para el Token) ---
                // Esta es la corrección más importante que soluciona el 401
                new Claim(ClaimTypes.Role, empleado.Rol!.Trim()),

                new Claim("centro_medico_id", empleado.CentroMedicoId!.Value.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}