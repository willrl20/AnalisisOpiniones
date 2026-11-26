using System.Diagnostics;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;

namespace AnalisisOpiniones.WkService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IExtractor<Encuesta> _csvExtractor;
        private readonly IExtractor<ResenaWeb> _dbExtractor;
        private readonly IExtractor<ComentarioSocial> _apiExtractor;
        private readonly IStagingRepository _staging;
        private readonly IEnumerable<IDimensionLoader> _cargadores;

        public Worker(ILogger<Worker> logger,
                      IExtractor<Encuesta> csvExtractor,
                      IExtractor<ResenaWeb> dbExtractor,
                      IExtractor<ComentarioSocial> apiExtractor,
                      IStagingRepository staging,
                      IEnumerable<IDimensionLoader> cargadores)
        {
            _logger = logger;
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _staging = staging;
            _cargadores = cargadores;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ExtraerAsync(stoppingToken);
                await CargarDimensionesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
        }

        private async Task ExtraerAsync(CancellationToken stoppingToken)
        {
            var cronometro = Stopwatch.StartNew();
            _logger.LogInformation("===== Inicio del proceso de extraccion: {Hora} =====", DateTime.Now);

            try
            {
                await _staging.LimpiarAsync();

                var tareaCsv = ExtraerSeguroAsync(_csvExtractor, stoppingToken);
                var tareaDb = ExtraerSeguroAsync(_dbExtractor, stoppingToken);
                var tareaApi = ExtraerSeguroAsync(_apiExtractor, stoppingToken);

                await Task.WhenAll(tareaCsv, tareaDb, tareaApi);

                await _staging.GuardarEncuestasAsync(tareaCsv.Result);
                await _staging.GuardarResenasAsync(tareaDb.Result);
                await _staging.GuardarComentariosAsync(tareaApi.Result);

                int total = tareaCsv.Result.Count() + tareaDb.Result.Count() + tareaApi.Result.Count();
                _logger.LogInformation("===== Extraccion finalizada: {Total} registros guardados en staging en {Ms} ms =====",
                    total, cronometro.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en el proceso de extraccion");
            }
        }

        private async Task CargarDimensionesAsync(CancellationToken stoppingToken)
        {
            var cronometroTotal = Stopwatch.StartNew();
            int totalInsertados = 0;
            _logger.LogInformation("===== Inicio de la carga de dimensiones =====");

            foreach (var cargador in _cargadores)
            {
                var cronometro = Stopwatch.StartNew();
                try
                {
                    var r = await cargador.CargarAsync(stoppingToken);
                    totalInsertados += r.Insertados;

                    _logger.LogInformation(
                        "{Dimension}: leidos {Leidos} | insertados {Insertados} | ya existentes {Existentes} | rechazados {Rechazados} | {Ms} ms",
                        r.Dimension, r.Leidos, r.Insertados, r.Existentes, r.Rechazados, cronometro.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cargar la dimension {Dimension}", cargador.NombreDimension);
                }
            }

            _logger.LogInformation("===== Carga de dimensiones finalizada: {Total} registros nuevos en {Ms} ms =====",
                totalInsertados, cronometroTotal.ElapsedMilliseconds);
        }

        private async Task<IEnumerable<T>> ExtraerSeguroAsync<T>(IExtractor<T> extractor, CancellationToken token)
        {
            var cronometro = Stopwatch.StartNew();
            try
            {
                var datos = (await extractor.ExtraerAsync(token)).ToList();
                _logger.LogInformation("{Fuente} completada en {Ms} ms", extractor.NombreFuente, cronometro.ElapsedMilliseconds);
                return datos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer de {Fuente}", extractor.NombreFuente);
                return Enumerable.Empty<T>();
            }
        }
    }
}