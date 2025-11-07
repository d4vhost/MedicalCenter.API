// Archivo: Controllers/AuthController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization; // <-- Asegúrate de que este 'using' esté presente

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

        [AllowAnonymous] // <-- ¡MODIFICACIÓN AÑADIDA!
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
                Rol = empleado.Rol,
                CentroMedicoId = empleado.CentroMedicoId.Value
            });
        }

        // --- NUEVO ENDPOINT SEGURO PARA VERIFICAR CÉDULA ---
        [Authorize(Roles = "ADMINISTRATIVO")] // Solo administradores pueden usar esto
        [HttpGet("CheckCedula/{cedula}")]
        public async Task<IActionResult> CheckCedula(string cedula)
        {
            var cedulaExists = await _globalContext.Empleados.AnyAsync(e => e.Cedula == cedula);
            // Si existe, necesitamos saber el ID para comparar si es el mismo empleado (en edición)
            if (cedulaExists)
            {
                var empleado = await _globalContext.Empleados
                    .Where(e => e.Cedula == cedula)
                    .Select(e => new { e.Id })
                    .FirstOrDefaultAsync();
                return Ok(empleado); // Retorna { "id": 123 }
            }
            return NotFound(); // Retorna 404 si no existe
        }
        // ---------------------------------------------------

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
        new Claim(ClaimTypes.Role, empleado.Rol!),
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