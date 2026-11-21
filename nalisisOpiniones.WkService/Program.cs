using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Extractors;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Repositories;
using AnalisisOpiniones.Data.Settings;
using AnalisisOpiniones.WkService;
using AnalisisOpiniones.WkService;

var builder = Host.CreateApplicationBuilder(args);

var settings = new FuentesSettings
{
    RutaCsvEncuestas = Path.Combine(AppContext.BaseDirectory, builder.Configuration["Fuentes:RutaCsvEncuestas"]!),
    UrlApiComentarios = builder.Configuration["Fuentes:UrlApiComentarios"]!,
    ConexionFuenteResenas = builder.Configuration.GetConnectionString("FuenteResenas")!,
    ConexionStaging = builder.Configuration.GetConnectionString("Staging")!
};

builder.Services.AddSingleton(settings);
builder.Services.AddTransient<IExtractor<Encuesta>, CsvExtractor>();
builder.Services.AddTransient<IExtractor<ResenaWeb>, DatabaseExtractor>();
builder.Services.AddHttpClient<IExtractor<ComentarioSocial>, ApiExtractor>();
builder.Services.AddTransient<IStagingRepository, StagingRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();