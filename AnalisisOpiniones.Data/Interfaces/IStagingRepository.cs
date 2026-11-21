using AnalisisOpiniones.Data.Entities;

namespace AnalisisOpiniones.Data.Interfaces
{
    public interface IStagingRepository
    {
        Task LimpiarAsync();
        Task GuardarEncuestasAsync(IEnumerable<Encuesta> encuestas);
        Task GuardarResenasAsync(IEnumerable<ResenaWeb> resenas);
        Task GuardarComentariosAsync(IEnumerable<ComentarioSocial> comentarios);
    }
}