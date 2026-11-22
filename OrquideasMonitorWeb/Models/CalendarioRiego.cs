namespace OrquideasMonitorWeb.Models
{
    public class CalendarioRiego
    {
        public int Id { get; set; }
        public string DiaSemana { get; set; } = string.Empty; 
        public TimeSpan HoraRiego { get; set; }
        public string Notas { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
