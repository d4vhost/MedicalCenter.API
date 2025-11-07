namespace MedicalCenter.API.Data
{
    public interface ILocalDbContextFactory
    {
        LocalDbContext CreateDbContext(int centroMedicoId);
    }
}
