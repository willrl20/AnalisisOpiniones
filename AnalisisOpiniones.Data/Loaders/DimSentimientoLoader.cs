using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Settings;

namespace AnalisisOpiniones.Data.Loaders
{
    public class DimSentimientoLoader : DimensionLoaderBase
    {
        private static readonly string[] Validos = { "Positiva", "Neutra", "Negativa", "Sin clasificar" };

        public DimSentimientoLoader(FuentesSettings settings) : base(settings) { }

        public override string NombreDimension => "DimSentimiento";

        public override async Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken)
        {
            var resultado = new ResultadoCarga { Dimension = NombreDimension };

            var origen = await LeerFilasAsync(Settings.ConexionDataWarehouse,
                @"SELECT Clasificacion FROM stg.Encuestas WHERE Clasificacion IS NOT NULL
                  UNION
                  SELECT CASE WHEN Rating >= 4 THEN 'Positiva' WHEN Rating = 3 THEN 'Neutra' ELSE 'Negativa' END
                  FROM stg.ResenasWeb WHERE Rating IS NOT NULL
                  UNION
                  SELECT 'Sin clasificar' FROM stg.ComentariosSociales", cancellationToken);

            var existentes = await LeerClavesAsync("SELECT Clasificacion FROM DimSentimiento", cancellationToken);

            var tabla = new DataTable("DimSentimiento");
            tabla.Columns.Add("Clasificacion", typeof(string));

            foreach (var f in origen)
            {
                resultado.Leidos++;
                string clasificacion = Convert.ToString(f[0])?.Trim() ?? "";

                if (!Validos.Contains(clasificacion))
                {
                    resultado.Rechazados++;
                    continue;
                }

                if (!existentes.Add(clasificacion))
                    continue;

                tabla.Rows.Add(clasificacion);
            }

            await InsertarMasivoAsync(tabla, cancellationToken);
            resultado.Insertados = tabla.Rows.Count;
            return resultado;
        }
    }
}