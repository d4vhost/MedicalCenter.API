namespace MedicalCenter.API.Data
{
    public interface ILocalDbContextFactory
    {
        // Crea un contexto basado en el ID del centro médico
        LocalDbContext CreateDbContext(int centroMedicoId);
    }
}