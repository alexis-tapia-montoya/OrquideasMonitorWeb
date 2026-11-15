namespace OrquideasMonitorWeb.Models
{
    public class SensorDispositivo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;     // Ej: "Invernadero Norte - DHT22"
        public string TipoConexion { get; set; } = "Wi-Fi";    // Wi-Fi o Bluetooth
        public string MacOIp { get; set; } = string.Empty;
        public bool Conectado { get; set; }
        public DateTime UltimaConexion { get; set; }
    }
}
