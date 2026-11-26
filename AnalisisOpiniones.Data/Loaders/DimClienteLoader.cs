using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Settings;

namespace AnalisisOpiniones.Data.Loaders
{
    public class DimClienteLoader : DimensionLoaderBase
    {
        public DimClienteLoader(FuentesSettings settings) : base(settings) { }

        public override string NombreDimension => "DimCliente";

        public override async Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken)
        {
            var resultado = new ResultadoCarga { Dimension = NombreDimension };

            var origen = await LeerFilasAsync(Settings.ConexionFuenteResenas,
                "SELECT IdCliente, Nombre, Email FROM Clientes", cancellationToken);

            var existentes = await LeerClavesAsync(
                "SELECT IdCliente FROM DimCliente WHERE IdCliente IS NOT NULL", cancellationToken);

            var anonimo = await LeerFilasAsync(Settings.ConexionDataWarehouse,
                "SELECT COUNT(*) FROM DimCliente WHERE IdCliente IS NULL", cancellationToken);

            var tabla = new DataTable("DimCliente");
            tabla.Columns.Add("IdCliente", typeof(int));
            tabla.Columns.Add("Nombre", typeof(string));
            tabla.Columns.Add("Email", typeof(string));
            tabla.Columns.Add("Pais", typeof(string));
            tabla.Columns.Add("RangoEdad", typeof(string));
            tabla.Columns.Add("TipoCliente", typeof(string));

            resultado.Leidos++;
            if (Convert.ToInt32(anonimo[0][0]) == 0)
                tabla.Rows.Add(DBNull.Value, "Anónimo", DBNull.Value, DBNull.Value, DBNull.Value, "Anónimo");

            foreach (var f in origen)
            {
                resultado.Leidos++;
                string nombre = Convert.ToString(f[1])?.Trim() ?? "";
                string email = Convert.ToString(f[2])?.Trim().ToLower() ?? "";

                if (f[0] == null || nombre == "" || !email.Contains('@'))
                {
                    resultado.Rechazados++;
                    continue;
                }

                if (!existentes.Add(f[0]!.ToString()!))
                    continue;

                tabla.Rows.Add(f[0], nombre, email, DBNull.Value, DBNull.Value, "Registrado");
            }

            await InsertarMasivoAsync(tabla, cancellationToken);
            resultado.Insertados = tabla.Rows.Count;
            return resultado;
        }
    }
}