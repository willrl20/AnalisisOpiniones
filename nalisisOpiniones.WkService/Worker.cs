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

        public Worker(ILogger<Worker> logger,
                      IExtractor<Encuesta> csvExtractor,
                      IExtractor<ResenaWeb> dbExtractor,
                      IExtractor<ComentarioSocial> apiExtractor,
                      IStagingRepository staging)
        {
            _logger = logger;
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _staging = staging;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
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
                    cronometro.Stop();

                    _logger.LogInformation("===== Extraccion finalizada: {Total} registros guardados en staging en {Ms} ms =====",
                        total, cronometro.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general en el proceso de extraccion");
                }

                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
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