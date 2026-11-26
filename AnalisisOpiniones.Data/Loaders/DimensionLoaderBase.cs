using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Settings;
using Microsoft.Data.SqlClient;

namespace AnalisisOpiniones.Data.Loaders
{
    public abstract class DimensionLoaderBase : IDimensionLoader
    {
        protected readonly FuentesSettings Settings;

        protected DimensionLoaderBase(FuentesSettings settings)
        {
            Settings = settings;
        }

        public abstract string NombreDimension { get; }

        public abstract Task<ResultadoCarga> CargarAsync(CancellationToken cancellationToken);

        protected async Task<List<object?[]>> LeerFilasAsync(string conexion, string consulta, CancellationToken cancellationToken)
        {
            var filas = new List<object?[]>();

            await using var cn = new SqlConnection(conexion);
            await cn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(consulta, cn);
            await using var dr = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await dr.ReadAsync(cancellationToken))
            {
                var valores = new object?[dr.FieldCount];
                for (int i = 0; i < dr.FieldCount; i++)
                    valores[i] = dr.IsDBNull(i) ? null : dr.GetValue(i);
                filas.Add(valores);
            }

            return filas;
        }

        protected async Task<HashSet<string>> LeerClavesAsync(string consulta, CancellationToken cancellationToken)
        {
            var filas = await LeerFilasAsync(Settings.ConexionDataWarehouse, consulta, cancellationToken);
            return filas.Select(f => Convert.ToString(f[0]) ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        protected async Task InsertarMasivoAsync(DataTable tabla, CancellationToken cancellationToken)
        {
            if (tabla.Rows.Count == 0) return;

            await using var cn = new SqlConnection(Settings.ConexionDataWarehouse);
            await cn.OpenAsync(cancellationToken);

            using var bulk = new SqlBulkCopy(cn) { DestinationTableName = tabla.TableName };
            foreach (DataColumn columna in tabla.Columns)
                bulk.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);

            await bulk.WriteToServerAsync(tabla, cancellationToken);
        }
    }
}