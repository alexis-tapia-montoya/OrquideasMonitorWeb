namespace OrquideasMonitorWeb.Models
{
    public class CalendarioRiego
    {
        public int Id { get; set; }
        public string DiaSemana { get; set; } = string.Empty; // Ej: "Lunes", "Miércoles", "Todos los días"
        public TimeSpan HoraRiego { get; set; }               // Ej: 08:30:00
        public string Notas { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
