using System.Data;
using AnalisisOpiniones.Data.Entities;
using AnalisisOpiniones.Data.Interfaces;
using AnalisisOpiniones.Data.Settings;
using Microsoft.Data.SqlClient;

namespace AnalisisOpiniones.Data.Repositories
{
    public class StagingRepository : IStagingRepository
    {
        private readonly FuentesSettings _settings;

        public StagingRepository(FuentesSettings settings)
        {
            _settings = settings;
        }

        public async Task LimpiarAsync()
        {
            await using var cn = new SqlConnection(_settings.ConexionStaging);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("stg.sp_LimpiarStaging", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            await cmd.ExecuteNonQueryAsync();
        }

        public Task GuardarEncuestasAsync(IEnumerable<Encuesta> encuestas)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("IdOpinion", typeof(int));
            tabla.Columns.Add("IdCliente", typeof(int));
            tabla.Columns.Add("IdProducto", typeof(int));
            tabla.Columns.Add("Fecha", typeof(DateTime));
            tabla.Columns.Add("Comentario", typeof(string));
            tabla.Columns.Add("Clasificacion", typeof(string));
            tabla.Columns.Add("Puntaje", typeof(int));
            tabla.Columns.Add("Fuente", typeof(string));

            foreach (var e in encuestas)
                tabla.Rows.Add(e.IdOpinion, e.IdCliente, e.IdProducto, e.Fecha, e.Comentario,
                               e.Clasificacion, (object?)e.Puntaje ?? DBNull.Value, e.Fuente);

            return InsertarMasivoAsync("stg.Encuestas", tabla);
        }

        public Task GuardarResenasAsync(IEnumerable<ResenaWeb> resenas)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("IdReview", typeof(string));
            tabla.Columns.Add("IdCliente", typeof(int));
            tabla.Columns.Add("IdProducto", typeof(int));
            tabla.Columns.Add("Fecha", typeof(DateTime));
            tabla.Columns.Add("Comentario", typeof(string));
            tabla.Columns.Add("Rating", typeof(int));

            foreach (var r in resenas)
                tabla.Rows.Add(r.IdReview, (object?)r.IdCliente ?? DBNull.Value, r.IdProducto, r.Fecha,
                               r.Comentario, (object?)r.Rating ?? DBNull.Value);

            return InsertarMasivoAsync("stg.ResenasWeb", tabla);
        }

        public Task GuardarComentariosAsync(IEnumerable<ComentarioSocial> comentarios)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("IdComment", typeof(string));
            tabla.Columns.Add("IdCliente", typeof(string));
            tabla.Columns.Add("IdProducto", typeof(string));
            tabla.Columns.Add("RedSocial", typeof(string));
            tabla.Columns.Add("Fecha", typeof(DateTime));
            tabla.Columns.Add("Comentario", typeof(string));

            foreach (var c in comentarios)
                tabla.Rows.Add(c.IdComment, (object?)c.IdCliente ?? DBNull.Value, c.IdProducto,
                               c.RedSocial, c.Fecha, c.Comentario);

            return InsertarMasivoAsync("stg.ComentariosSociales", tabla);
        }

        private async Task InsertarMasivoAsync(string tablaDestino, DataTable datos)
        {
            await using var cn = new SqlConnection(_settings.ConexionStaging);
            await cn.OpenAsync();

            using var bulk = new SqlBulkCopy(cn) { DestinationTableName = tablaDestino };
            foreach (DataColumn columna in datos.Columns)
                bulk.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);

            await bulk.WriteToServerAsync(datos);
        }
    }
}