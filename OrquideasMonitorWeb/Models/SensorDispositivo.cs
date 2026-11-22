namespace OrquideasMonitorWeb.Models
{
    public class SensorDispositivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;    
        public string TipoConexion { get; set; } = "Wi-Fi";   
        public string MacOIp { get; set; } = string.Empty;
        public bool Conectado { get; set; }
        public DateTime UltimaConexion { get; set; }
    }
}
