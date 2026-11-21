namespace AnalisisOpiniones.Api.Models
{
    public class ComentarioSocialDto
    {
        public string IdComment { get; set; } = "";
        public string? IdCliente { get; set; }
        public string IdProducto { get; set; } = "";
        public string RedSocial { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = "";
    }
}