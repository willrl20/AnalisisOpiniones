using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Settings;

namespace AnalisisOpiniones.Data.Loaders
{
    public class DimFuenteLoader : DimensionLoaderBase
    {
        private static readonly string[] CanalesValidos = { "Encuesta", "Web", "Red Social" };

        public DimFuenteLoader(FuentesSettings settings) : base(settings) { }

        public override string NombreDimension => "DimFuente";

        public override async Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken)
        {
            var resultado = new ResultadoCarga { Dimension = NombreDimension };

            var origen = await LeerFilasAsync(Settings.ConexionDataWarehouse,
                @"SELECT DISTINCT 'Encuesta', 'Encuesta interna', 'Archivo CSV' FROM stg.Encuestas
                  UNION SELECT DISTINCT 'Web', 'Sitio web', 'Base de datos relacional' FROM stg.ResenasWeb
                  UNION SELECT DISTINCT 'Red Social', RedSocial, 'API REST' FROM stg.ComentariosSociales", cancellationToken);

            var existentes = await LeerClavesAsync("SELECT Canal + '|' + Plataforma FROM DimFuente", cancellationToken);

            var tabla = new DataTable("DimFuente");
            tabla.Columns.Add("Canal", typeof(string));
            tabla.Columns.Add("Plataforma", typeof(string));
            tabla.Columns.Add("TipoOrigen", typeof(string));

            foreach (var f in origen)
            {
                resultado.Leidos++;
                string canal = Convert.ToString(f[0])?.Trim() ?? "";
                string plataforma = Convert.ToString(f[1])?.Trim() ?? "";

                if (!CanalesValidos.Contains(canal) || plataforma == "")
                {
                    resultado.Rechazados++;
                    continue;
                }

                if (!existentes.Add(canal + "|" + plataforma))
                    continue;

                tabla.Rows.Add(canal, plataforma, f[2]);
            }

            await InsertarMasivoAsync(tabla, cancellationToken);
            resultado.Insertados = tabla.Rows.Count;
            return resultado;
        }
    }
}