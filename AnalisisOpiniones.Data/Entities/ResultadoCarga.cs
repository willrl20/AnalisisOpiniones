namespace AnalisisOpiniones.Data.Entities
{
    public class ResultadoCarga
    {
        public string Dimension { get; set; } = "";
        public int Leidos { get; set; }
        public int Insertados { get; set; }
        public int Rechazados { get; set; }
        public int Existentes => Leidos - Insertados - Rechazados;
    }
}