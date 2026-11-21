using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.Data.Extractors
{
    public class DatabaseExtractor : IExtractor<ResenaWeb>
    {
        private readonly FuentesSettings _settings;
        private readonly ILogger<DatabaseExtractor> _logger;

        private const string Consulta = @"
            SELECT r.CodigoReview, o.IdCliente, o.IdProducto, o.Fecha, o.Comentario, o.Puntaje
            FROM ResenasWeb r
            INNER JOIN Opiniones o ON o.IdOpinion = r.IdOpinion";

        public DatabaseExtractor(FuentesSettings settings, ILogger<DatabaseExtractor> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public string NombreFuente => "Base de datos - Resenas web";

        public async Task<IEnumerable<ResenaWeb>> ExtraerAsync(CancellationToken cancellationToken)
        {
            var resenas = new List<ResenaWeb>();

            await using var cn = new SqlConnection(_settings.ConexionFuenteResenas);
            await cn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(Consulta, cn);
            await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await dr.ReadAsync(cancellationToken))
            {
                resenas.Add(new ResenaWeb
                {
                    IdReview = dr.GetString(0),
                    IdCliente = dr.IsDBNull(1) ? null : dr.GetInt32(1),
                    IdProducto = dr.GetInt32(2),
                    Fecha = dr.GetDateTime(3),
                    Comentario = dr.GetString(4),
                    Rating = dr.IsDBNull(5) ? null : dr.GetByte(5)
                });
            }

            _logger.LogInformation("{Fuente}: {Cantidad} registros extraidos", NombreFuente, resenas.Count);
            return resenas;
        }
    }
}