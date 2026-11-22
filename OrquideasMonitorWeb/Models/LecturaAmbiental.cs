namespace OrquideasMonitorWeb.Models
{
    public class LecturaAmbiental
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public double Temperatura { get; set; } 
        public double Humedad { get; set; } 
        public DateTime FechaRegistro { get; set; }
        public bool FueraDeRango { get; set; }
    }
}
