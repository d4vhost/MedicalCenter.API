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
            string connectionString;

            // Lógica para seleccionar la cadena de conexión
            // (Asumimos 1=Quito(Global), 2=Guayaquil, 3=Cuenca)
            // Esta lógica es un ejemplo, ajústala a tus IDs de Centros Médicos
            switch (centroMedicoId)
            {
                case 2: // ID de Guayaquil
                    connectionString = _configuration.GetConnectionString("GuayaquilDb");
                    break;
                case 3: // ID de Cuenca
                    connectionString = _configuration.GetConnectionString("CuencaDb");
                    break;
                default:
                    // Si es Quito (ID 1) o un admin sin centro, no deberían escribir datos locales
                    // O puedes tener una lógica por defecto.
                    throw new Exception("Centro Médico no tiene base de datos local.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new LocalDbContext(optionsBuilder.Options);
        }
    }
}
