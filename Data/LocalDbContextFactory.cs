using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace MedicalCenter.API.Data
{
    public class LocalDbContextFactory : ILocalDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public LocalDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LocalDbContext CreateDbContext(int centroMedicoId)
        {
            // 1. La variable ahora es 'string?' (nullable)
            string? connectionString;

            switch (centroMedicoId)
            {
                case 2: // ID de Guayaquil
                    connectionString = _configuration.GetConnectionString("GuayaquilDb");
                    break;
                case 3: // ID de Cuenca
                    connectionString = _configuration.GetConnectionString("CuencaDb");
                    break;
                default:
                    // 2. Manejamos el caso por defecto (ej. Admin o ID inválido)
                    throw new ArgumentException($"Centro Médico ID no válido o no tiene base de datos local: {centroMedicoId}");
            }

            // 3. Verificamos si la cadena de conexión es nula O vacía
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"No se encontró la cadena de conexión para el Centro Médico ID: {centroMedicoId}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new LocalDbContext(optionsBuilder.Options);
        }
    }
}