// --- IMPORTACIONES NECESARIAS ---
using MedicalCenter.API.Data; // Para nuestros nuevos DbContexts y la Fábrica
using Microsoft.EntityFrameworkCore; // Para Entity Framework Core
using Microsoft.AspNetCore.Authentication.JwtBearer; // Para la autenticación JWT
using Microsoft.IdentityModel.Tokens; // Para la validación de Tokens
using System.Text; // Para la codificación de la clave JWT

var builder = WebApplication.CreateBuilder(args);

// --- 1. Agregar servicios al contenedor ---

// Configuración de CORS para permitir solicitudes desde tu frontend (Vue)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "https://medicalcenterbd.netlify.app", // 1. Tu Frontend PUBLICADO (Producción)
                    "http://localhost:5173"                // 2. Tu Frontend LOCAL (Para seguir desarrollando)
                   )
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddControllers();

// --- 2. CONFIGURACIÓN DE LA BASE DE DATOS (AQUÍ ESTÁ EL CAMBIO) ---

// 2.1. Registra el CONTEXTO GLOBAL (Se conecta siempre a 'centro-quito')
// Lee la cadena de conexión "GlobalDb" desde appsettings.json
var globalConnectionString = builder.Configuration.GetConnectionString("GlobalDb");
builder.Services.AddDbContext<GlobalDbContext>(options =>
    options.UseMySql(globalConnectionString, ServerVersion.AutoDetect(globalConnectionString)));

// 2.2. Registra la FÁBRICA para los CONTEXTOS LOCALES
// Usamos AddScoped para que se cree una nueva instancia de la fábrica 
// para cada solicitud HTTP.
builder.Services.AddScoped<ILocalDbContextFactory, LocalDbContextFactory>();

// --- FIN DE LA CONFIGURACIÓN DE LA BASE DE DATOS ---


// --- 3. Configuración de Autenticación JWT ---

// 3.1. LEEMOS Y VALIDAMOS LA CLAVE JWT (¡ESTA ES LA CORRECCIÓN!)
// Declaramos 'jwtKey' en el ámbito principal.
var jwtKey = builder.Configuration["JWT:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    // Si la clave no está en appsettings.json, la aplicación no se iniciará.
    throw new InvalidOperationException("No se ha configurado la clave JWT (JWT:Key) en appsettings.json.");
}

// 3.2. Esto "enseña" a la API a leer y validar los tokens JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            // Ahora 'jwtKey' SÍ existe y es visible.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 4. Configuración de Swagger (para la documentación de la API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- Construir la aplicación ---
var app = builder.Build();

// --- 5. Configurar el pipeline de solicitudes HTTP ---

// Habilitar Swagger solo en modo de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Habilita el enrutamiento
app.UseRouting();

// Habilita la política de CORS que definimos arriba
app.UseCors("AllowFrontend"); // El '!' es para silenciar la advertencia (sabemos que no es nulo)

// ¡¡MUY IMPORTANTE!! Habilitar Autenticación y Autorización
// Deben ir en este orden (primero autenticar, luego autorizar)
// y deben estar después de UseRouting y antes de MapControllers.
app.UseAuthentication();
app.UseAuthorization();

// Mapea los controladores que definiste
app.MapControllers();

// Ejecuta la aplicación
app.Run();