using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexi�n
var connectionString = builder.Configuration.GetConnectionString("QuitoConnection");

// 2. Configurar el DbContext
builder.Services.AddDbContext<MedicalCenterDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Add services to the container.
builder.Services.AddControllers();

// 3. A�ADIR CONFIGURACI�N DE AUTENTICACI�N JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // --- SOLUCI�N: Verificamos que la clave JWT no sea nula ---
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new ArgumentNullException("Jwt:Key", "La clave JWT no se encontr� en la configuraci�n.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) // Usamos la variable verificada
        };
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. ACTIVAR LA AUTENTICACI�N Y AUTORIZACI�N
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();