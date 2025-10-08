using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCenter.API.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly MedicalCenterDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(MedicalCenterDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto loginRequest)
    {
        // La búsqueda ahora es solo por cédula
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

        // La validación ya no comprueba el centro médico
        if (empleado == null || empleado.Password != loginRequest.Password)
        {
            return Unauthorized("Usuario o contraseña incorrectos.");
        }

        var token = GenerateJwtToken(empleado);

        var response = new LoginResponseDto
        {
            Token = token,
            EmpleadoId = empleado.Id,
            NombreCompleto = $"{empleado.Nombre} {empleado.Apellido}",
            Rol = empleado.Rol // El rol se envía en la respuesta, ¡esto es clave!
        };

        return Ok(response);
    }

    [HttpPost("login-paciente")]
    public async Task<ActionResult<LoginResponseDto>> LoginPaciente(PacienteLoginRequestDto loginRequest)
    {
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p => p.Cedula == loginRequest.Cedula && p.FechaNacimiento.HasValue && p.FechaNacimiento.Value.Date == loginRequest.FechaNacimiento.Date);

        if (paciente == null)
        {
            return Unauthorized("Cédula o fecha de nacimiento incorrectas.");
        }

        // Simulando un token para el paciente
        var token = GenerateJwtTokenForPaciente(paciente);

        var response = new LoginResponseDto
        {
            Token = token,
            EmpleadoId = paciente.Id, // Usamos el Id del paciente
            NombreCompleto = $"{paciente.Nombre} {paciente.Apellido}",
            Rol = "Paciente"
        };

        return Ok(response);
    }

    private string GenerateJwtTokenForPaciente(Paciente paciente)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "La clave JWT no se encontró en la configuración.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, paciente.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.GivenName, paciente.Nombre),
        new Claim(ClaimTypes.Role, "Paciente"),
    };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateJwtToken(Empleado empleado)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "La clave JWT no se encontró en la configuración.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, empleado.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.GivenName, empleado.Nombre),
            new Claim(ClaimTypes.Role, empleado.Rol ?? "Empleado"),
            new Claim("centroId", empleado.CentroMedicoId?.ToString() ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}