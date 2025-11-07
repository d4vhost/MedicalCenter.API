// Archivo: Controllers/AuthController.cs

using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MedicalCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GlobalDbContext _globalContext;

        // --- CORRECCIÓN 1: Declarar el campo ---
        // Este campo no existía, por eso tenías el error CS0103
        private readonly IConfiguration _configuration;

        public AuthController(GlobalDbContext globalContext, IConfiguration configuration)
        {
            _globalContext = globalContext;

            // --- CORRECCIÓN 2: Asignar el valor ---
            // Faltaba esta línea para guardar la configuración
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            var empleado = await _globalContext.Empleados
                .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

            if (empleado == null || empleado.Password != loginRequest.Password)
            {
                return Unauthorized(new { message = "Cédula o contraseña incorrecta." });
            }

            // --- CORRECCIÓN 3: Validar nulos (para error CS0029) ---
            // Verificamos que el empleado tenga un rol y un centro asignados.
            // Si son nulos, la cuenta no es válida para iniciar sesión.
            if (string.IsNullOrEmpty(empleado.Rol) || empleado.CentroMedicoId == null)
            {
                return Unauthorized(new { message = "La cuenta del empleado no está configurada correctamente (falta Rol o Centro Médico)." });
            }

            // Si llegamos aquí, sabemos que .Rol y .CentroMedicoId NO son nulos.
            var token = GenerateJwtToken(empleado);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Id = empleado.Id,
                Nombre = empleado.Nombre,
                Apellido = empleado.Apellido,
                // Ahora la asignación es válida porque ya comprobamos que no son nulos
                Rol = empleado.Rol,
                CentroMedicoId = empleado.CentroMedicoId.Value // Usamos .Value para obtener el 'int' del 'int?'
            });
        }

        private string GenerateJwtToken(Empleado empleado)
        {
            // Esta línea ahora funciona porque _configuration ya existe
            var jwtKey = _configuration["JWT:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key no está configurada en appsettings.json");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // --- CORRECCIÓN 4: Usar los valores validados ---
            // Le decimos al compilador que confiamos que Rol y CentroMedicoId no son nulos
            // (porque lo validamos en el método Login)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.GivenName, empleado.Nombre),
                new Claim(ClaimTypes.Surname, empleado.Apellido),
                new Claim(ClaimTypes.Role, empleado.Rol!), // El '!' le dice al compilador: "Confía en mí, no es nulo"
                new Claim("centro_medico_id", empleado.CentroMedicoId!.Value.ToString()) // Usamos .Value
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                // Estas líneas ahora funcionan porque _configuration ya existe
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