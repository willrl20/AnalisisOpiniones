using System.Data;
using System.Globalization;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Settings;

namespace AnalisisOpiniones.Data.Loaders
{
    public class DimFechaLoader : DimensionLoaderBase
    {
        private static readonly CultureInfo Cultura = new CultureInfo("es-ES");

        public DimFechaLoader(FuentesSettings settings) : base(settings) { }

        public override string NombreDimension => "DimFecha";

        public override async Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken)
        {
            var resultado = new ResultadoCarga { Dimension = NombreDimension };

            var rango = await LeerFilasAsync(Settings.ConexionDataWarehouse,
                @"SELECT MIN(Fecha), MAX(Fecha) FROM (
                      SELECT Fecha FROM stg.Encuestas
                      UNION ALL SELECT Fecha FROM stg.ResenasWeb
                      UNION ALL SELECT Fecha FROM stg.ComentariosSociales) t", cancellationToken);

            if (rango[0][0] == null)
                return resultado;

            var inicio = (DateTime)rango[0][0]!;
            var fin = (DateTime)rango[0][1]!;

            var existentes = await LeerClavesAsync("SELECT FechaKey FROM DimFecha", cancellationToken);

            var tabla = new DataTable("DimFecha");
            tabla.Columns.Add("FechaKey", typeof(int));
            tabla.Columns.Add("Fecha", typeof(DateTime));
            tabla.Columns.Add("Dia", typeof(byte));
            tabla.Columns.Add("Mes", typeof(byte));
            tabla.Columns.Add("NombreMes", typeof(string));
            tabla.Columns.Add("Trimestre", typeof(byte));
            tabla.Columns.Add("Anio", typeof(short));
            tabla.Columns.Add("DiaSemana", typeof(string));
            tabla.Columns.Add("Evento", typeof(string));

            for (var f = inicio; f <= fin; f = f.AddDays(1))
            {
                resultado.Leidos++;
                int fechaKey = f.Year * 10000 + f.Month * 100 + f.Day;

                if (!existentes.Add(fechaKey.ToString()))
                    continue;

                tabla.Rows.Add(fechaKey, f, (byte)f.Day, (byte)f.Month,
                    Cultura.TextInfo.ToTitleCase(Cultura.DateTimeFormat.GetMonthName(f.Month)),
                    (byte)((f.Month - 1) / 3 + 1), (short)f.Year,
                    Cultura.TextInfo.ToTitleCase(Cultura.DateTimeFormat.GetDayName(f.DayOfWeek)),
                    (object?)ObtenerEvento(f) ?? DBNull.Value);
            }

            await InsertarMasivoAsync(tabla, cancellationToken);
            resultado.Insertados = tabla.Rows.Count;
            return resultado;
        }

        private static string? ObtenerEvento(DateTime f)
        {
            if (f.Month == 12 && f.Day == 25) return "Navidad";
            if (f.Month == 1 && f.Day == 1) return "Año Nuevo";
            if (f.Month == 11 && f.DayOfWeek == DayOfWeek.Friday && f.Day >= 23 && f.Day <= 29) return "Black Friday";
            return null;
        }
    }
}