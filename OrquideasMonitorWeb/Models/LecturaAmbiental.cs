namespace OrquideasMonitorWeb.Models
{
    public class LecturaAmbiental
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public double Temperatura { get; set; } // Óptimo: 18°C - 24°C
        public double Humedad { get; set; }     // Óptimo: ~80%
        public DateTime FechaRegistro { get; set; }
        public bool FueraDeRango { get; set; }
    }
}
