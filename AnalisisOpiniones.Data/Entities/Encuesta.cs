namespace AnalisisOpiniones.Data.Entities
{
    public class Encuesta
    {
        public int IdOpinion { get; set; }
        public int IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = "";
        public string? Clasificacion { get; set; }
        public int? Puntaje { get; set; }
        public string? Fuente { get; set; }
    }
}