using Microsoft.AspNetCore.Mvc;
using OrquideasMonitorWeb.Models;

namespace OrquideasMonitorWeb.Controllers
{
    public class InvernaderoController : Controller
    {
        // Almacenamiento temporal en memoria (sin base de datos por ahora)
        private static List<CalendarioRiego> _riegos = new();
        private static List<SensorDispositivo> _sensores = new()
        {
            new SensorDispositivo
            {
                Id = 1,
                Nombre = "Sensor Principal DHT22",
                TipoConexion = "Wi-Fi",
                MacOIp = "192.168.1.50",
                Conectado = true,
                UltimaConexion = DateTime.Now
            }
        };
        private static LecturaAmbiental? _ultimaLectura = new()
        {
            Id = 1,
            SensorId = 1,
            Temperatura = 21.0,
            Humedad = 80.0,
            FechaRegistro = DateTime.Now,
            FueraDeRango = false
        };

        // Vista principal: RF1 (Calendario), RF3 (Sensores) y RF4 (Monitoreo actual)
        public IActionResult Index()
        {
            ViewBag.Riegos = _riegos.Where(r => r.Activo).OrderBy(r => r.HoraRiego).ToList();
            ViewBag.Sensores = _sensores;
            return View(_ultimaLectura);
        }

        // RF1: Guardar nuevo horario de riego
        [HttpPost]
        public IActionResult AgregarRiego(string diaSemana, TimeSpan horaRiego, string notas)
        {
            _riegos.Add(new CalendarioRiego
            {
                Id = _riegos.Count + 1,
                DiaSemana = diaSemana,
                HoraRiego = horaRiego,
                Notas = notas ?? "",
                Activo = true
            });

            return RedirectToAction("Index");
        }

        // RF1: Eliminar horario de riego
        [HttpPost]
        public IActionResult EliminarRiego(int id)
        {
            var riego = _riegos.FirstOrDefault(r => r.Id == id);
            if (riego != null)
            {
                _riegos.Remove(riego);
            }

            return RedirectToAction("Index");
        }

        // RF3: Emparejar/Conectar sensor Wi-Fi o Bluetooth
        [HttpPost]
        public IActionResult ConectarSensor(string nombre, string tipoConexion, string macOIp)
        {
            _sensores.Add(new SensorDispositivo
            {
                Id = _sensores.Count + 1,
                Nombre = nombre,
                TipoConexion = tipoConexion,
                MacOIp = macOIp,
                Conectado = true,
                UltimaConexion = DateTime.Now
            });

            return RedirectToAction("Index");
        }

        // RF4: Endpoint que recibe datos del sensor o del botón simulador
        [HttpPost]
        [Route("api/iot/lectura")]
        public IActionResult RecibirLectura([FromBody] LecturaAmbiental datos)
        {
            // Validar rangos óptimos de orquídeas: Humedad ~80% (75%-85%) y Temp 18°C - 24°C
            bool fueraDeRango = (datos.Temperatura < 18.0 || datos.Temperatura > 24.0) ||
                                (datos.Humedad < 75.0 || datos.Humedad > 85.0);

            _ultimaLectura = new LecturaAmbiental
            {
                Id = (_ultimaLectura?.Id ?? 0) + 1,
                SensorId = datos.SensorId == 0 ? 1 : datos.SensorId,
                Temperatura = datos.Temperatura,
                Humedad = datos.Humedad,
                FechaRegistro = DateTime.Now,
                FueraDeRango = fueraDeRango
            };

            return Ok(new { ok = true, fueraDeRango });
        }

        // RF2 & RF4: Consulta en vivo para notificaciones de riego y alertas climáticas
        [HttpGet]
        [Route("api/iot/monitoreo")]
        public IActionResult MonitoreoEnVivo()
        {
            var ahora = DateTime.Now;
            var riegoActual = _riegos.FirstOrDefault(r =>
                r.Activo && r.HoraRiego.Hours == ahora.Hour && r.HoraRiego.Minutes == ahora.Minute);

            return Ok(new
            {
                lectura = _ultimaLectura,
                esHoraDeRegar = riegoActual != null,
                mensajeRiego = riegoActual?.Notas ?? ""
            });
        }
    }
}