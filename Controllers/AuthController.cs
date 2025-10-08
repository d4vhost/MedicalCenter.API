using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCenter.API.Data;
using MedicalCenter.API.Models.DTOs;
using MedicalCenter.API.Models.Entities;
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
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Cedula == loginRequest.Cedula);

        if (empleado == null || empleado.Password != loginRequest.Password || empleado.CentroMedicoId != loginRequest.CentroMedicoId)
        {
            return Unauthorized("Cédula, contraseña o centro médico incorrectos.");
        }

        var token = GenerateJwtToken(empleado);

        var response = new LoginResponseDto
        {
            Token = token,
            EmpleadoId = empleado.Id,
            NombreCompleto = $"{empleado.Nombre} {empleado.Apellido}",
            Rol = empleado.Rol
        };

        return Ok(response);
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
            // SOLUCIÓN: Proporcionamos un valor por defecto si Rol es nulo
            new Claim(ClaimTypes.Role, empleado.Rol ?? "Empleado"),
            // SOLUCIÓN: Convertimos de forma segura int? a string
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