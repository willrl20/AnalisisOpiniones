using System.Globalization;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Settings;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.Data.Extractors
{
    public class CsvExtractor : IExtractor<Encuesta>
    {
        private readonly FuentesSettings _settings;
        private readonly ILogger<CsvExtractor> _logger;

        public CsvExtractor(FuentesSettings settings, ILogger<CsvExtractor> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public string NombreFuente => "CSV - Encuestas internas";

        public async Task<IEnumerable<Encuesta>> ExtraerAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_settings.RutaCsvEncuestas))
                throw new FileNotFoundException("No se encontro el archivo CSV", _settings.RutaCsvEncuestas);

            var encuestas = new List<Encuesta>();

            using var reader = new StreamReader(_settings.RutaCsvEncuestas);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<EncuestaMap>();

            await foreach (var encuesta in csv.GetRecordsAsync<Encuesta>(cancellationToken))
            {
                encuestas.Add(encuesta);
            }

            _logger.LogInformation("{Fuente}: {Cantidad} registros extraidos", NombreFuente, encuestas.Count);
            return encuestas;
        }
    }

    public sealed class EncuestaMap : ClassMap<Encuesta>
    {
        public EncuestaMap()
        {
            Map(e => e.IdOpinion).Name("IdOpinion");
            Map(e => e.IdCliente).Name("IdCliente");
            Map(e => e.IdProducto).Name("IdProducto");
            Map(e => e.Fecha).Name("Fecha");
            Map(e => e.Comentario).Name("Comentario");
            Map(e => e.Clasificacion).Name("Clasificación");
            Map(e => e.Puntaje).Name("PuntajeSatisfacción");
            Map(e => e.Fuente).Name("Fuente");
        }
    }
}