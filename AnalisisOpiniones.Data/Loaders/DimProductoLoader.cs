using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Settings;

namespace AnalisisOpiniones.Data.Loaders
{
    public class DimProductoLoader : DimensionLoaderBase
    {
        public DimProductoLoader(FuentesSettings settings) : base(settings) { }

        public override string NombreDimension => "DimProducto";

        public override async Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken)
        {
            var resultado = new ResultadoCarga { Dimension = NombreDimension };

            var origen = await LeerFilasAsync(Settings.ConexionFuenteResenas,
                @"SELECT p.IdProducto, p.Nombre, c.Nombre
                  FROM Productos p
                  INNER JOIN Categorias c ON c.IdCategoria = p.IdCategoria", cancellationToken);

            var existentes = await LeerClavesAsync("SELECT IdProducto FROM DimProducto", cancellationToken);

            var tabla = new DataTable("DimProducto");
            tabla.Columns.Add("IdProducto", typeof(int));
            tabla.Columns.Add("Nombre", typeof(string));
            tabla.Columns.Add("Categoria", typeof(string));

            foreach (var f in origen)
            {
                resultado.Leidos++;
                string nombre = Convert.ToString(f[1])?.Trim() ?? "";
                string categoria = Convert.ToString(f[2])?.Trim() ?? "";

                if (f[0] == null || nombre == "" || categoria == "")
                {
                    resultado.Rechazados++;
                    continue;
                }

                if (!existentes.Add(f[0]!.ToString()!))
                    continue;

                tabla.Rows.Add(f[0], nombre, categoria);
            }

            await InsertarMasivoAsync(tabla, cancellationToken);
            resultado.Insertados = tabla.Rows.Count;
            return resultado;
        }
    }
}