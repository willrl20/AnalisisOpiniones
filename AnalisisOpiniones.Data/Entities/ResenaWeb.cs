namespace AnalisisOpiniones.Data.Entities
{
    public class ResenaWeb
    {
        public string IdReview { get; set; } = "";
        public int? IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = "";
        public int? Rating { get; set; }
    }
}