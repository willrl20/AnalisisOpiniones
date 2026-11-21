using AnalisisOpiniones.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;

namespace AnalisisOpiniones.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComentariosController : ControllerBase
    {
        private readonly ILogger<ComentariosController> _logger;

        public ComentariosController(ILogger<ComentariosController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ComentarioSocialDto>> Get()
        {
            string ruta = Path.Combine(AppContext.BaseDirectory, "Datos", "social_comments.csv");

            if (!System.IO.File.Exists(ruta))
                return NotFound("No se encontro la fuente de comentarios.");

            var comentarios = new List<ComentarioSocialDto>();

            using var parser = new TextFieldParser(ruta);
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.ReadFields();

            while (!parser.EndOfData)
            {
                var f = parser.ReadFields()!;
                comentarios.Add(new ComentarioSocialDto
                {
                    IdComment = f[0],
                    IdCliente = string.IsNullOrWhiteSpace(f[1]) ? null : f[1],
                    IdProducto = f[2],
                    RedSocial = f[3],
                    Fecha = DateTime.Parse(f[4]),
                    Comentario = f[5]
                });
            }

            _logger.LogInformation("Se entregaron {Cantidad} comentarios", comentarios.Count);
            return Ok(comentarios);
        }
    }
}