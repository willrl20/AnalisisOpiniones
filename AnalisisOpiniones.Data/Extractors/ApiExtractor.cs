using System.Net.Http.Json;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Settings;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.Data.Extractors
{
    public class ApiExtractor : IExtractor<ComentarioSocial>
    {
        private readonly HttpClient _httpClient;
        private readonly FuentesSettings _settings;
        private readonly ILogger<ApiExtractor> _logger;

        public ApiExtractor(HttpClient httpClient, FuentesSettings settings, ILogger<ApiExtractor> logger)
        {
            _httpClient = httpClient;
            _settings = settings;
            _logger = logger;
        }

        public string NombreFuente => "API REST - Comentarios sociales";

        public async Task<IEnumerable<ComentarioSocial>> ExtraerAsync(CancellationToken cancellationToken)
        {
            var comentarios = await _httpClient.GetFromJsonAsync<List<ComentarioSocial>>(
                _settings.UrlApiComentarios, cancellationToken);

            var resultado = comentarios ?? new List<ComentarioSocial>();
            _logger.LogInformation("{Fuente}: {Cantidad} registros extraidos", NombreFuente, resultado.Count);
            return resultado;
        }
    }
}