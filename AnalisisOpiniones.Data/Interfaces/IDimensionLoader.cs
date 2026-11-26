using AnalisisOpiniones.Data.Entities;

namespace AnalisisOpiniones.Data.Interfaces
{
    public interface IDimensionLoader
    {
        string NombreDimension { get; }
        Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken);
    }
}