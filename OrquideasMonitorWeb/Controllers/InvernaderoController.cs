using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OrquideasMonitorWeb.Models;

namespace OrquideasMonitorWeb.Controllers
{
    [Authorize]
    public class InvernaderoController : Controller
    {
        private readonly string _connectionString;

        public InvernaderoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // Vista principal: RF1 al RF8 integrados con MySQL
        public async Task<IActionResult> Index()
        {
            var riegos = new List<CalendarioRiego>();
            var sensores = new List<SensorDispositivo>();
            var historial = new List<LecturaAmbiental>();
            int frecuenciaMinutos = 60;

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // RF1: Cargar calendario de riego
            using (var cmd = new MySqlCommand("SELECT * FROM CalendarioRiego WHERE Activo = 1 ORDER BY HoraRiego ASC", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    riegos.Add(new CalendarioRiego
                    {
                        Id = reader.GetInt32("Id"),
                        DiaSemana = reader.GetString("DiaSemana"),
                        HoraRiego = reader.GetTimeSpan("HoraRiego"),
                        Notas = reader.IsDBNull(reader.GetOrdinal("Notas")) ? "" : reader.GetString("Notas"),
                        Activo = reader.GetBoolean("Activo")
                    });
                }
            }

            // RF3: Cargar sensores vinculados
            using (var cmd = new MySqlCommand("SELECT * FROM Sensores", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    sensores.Add(new SensorDispositivo
                    {
                        Id = reader.GetInt32("Id"),
                        Nombre = reader.GetString("Nombre"),
                        TipoConexion = reader.GetString("TipoConexion"),
                        MacOIp = reader.GetString("MacOIp"),
                        Conectado = reader.GetBoolean("Conectado"),
                        UltimaConexion = reader.GetDateTime("UltimaConexion")
                    });
                }
            }

            // RF5, RF6 & RF8: Cargar últimas 30 lecturas históricas para gráfico y lectura actual
            using (var cmd = new MySqlCommand("SELECT * FROM LecturasAmbientales ORDER BY FechaRegistro DESC, Id DESC LIMIT 30", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    historial.Add(new LecturaAmbiental
                    {
                        Id = reader.GetInt32("Id"),
                        SensorId = reader.GetInt32("SensorId"),
                        Temperatura = reader.GetDouble("Temperatura"),
                        Humedad = reader.GetDouble("Humedad"),
                        FechaRegistro = reader.GetDateTime("FechaRegistro"),
                        FueraDeRango = reader.GetBoolean("FueraDeRango")
                    });
                }
            }

            // RF5: Obtener frecuencia histórica configurada
            using (var cmd = new MySqlCommand("SELECT FrecuenciaMinutos FROM ConfiguracionSistema WHERE Id = 1", conn))
            {
                var res = await cmd.ExecuteScalarAsync();
                if (res != null) frecuenciaMinutos = Convert.ToInt32(res);
            }

            // Administración de usuarios (solo si el usuario autenticado es admin)
            var usuariosSistema = new List<dynamic>();
            if (User.FindFirst("Usuario")?.Value == "admin")
            {
                using var cmdUsers = new MySqlCommand(
                    "SELECT Id, NombreCompleto, Usuario, (DescriptorFacial IS NOT NULL) AS TieneRostro FROM Usuarios WHERE Activo = 1 ORDER BY Id ASC", conn);
                using var readerUsers = await cmdUsers.ExecuteReaderAsync();
                while (await readerUsers.ReadAsync())
                {
                    usuariosSistema.Add(new
                    {
                        Id = readerUsers.GetInt32("Id"),
                        NombreCompleto = readerUsers.GetString("NombreCompleto"),
                        Usuario = readerUsers.GetString("Usuario"),
                        TieneRostro = readerUsers.GetBoolean("TieneRostro")
                    });
                }
            }

            ViewBag.Usuarios = usuariosSistema;
            ViewBag.Riegos = riegos;
            ViewBag.Sensores = sensores;
            ViewBag.Historial = historial;
            ViewBag.FrecuenciaMinutos = frecuenciaMinutos;

            return View(historial.FirstOrDefault());
        }

        // RF1: Guardar nuevo horario de riego
        [HttpPost]
        public async Task<IActionResult> AgregarRiego(string diaSemana, TimeSpan horaRiego, string notas)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = "INSERT INTO CalendarioRiego (DiaSemana, HoraRiego, Notas, Activo) VALUES (@dia, @hora, @notas, 1)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@dia", diaSemana);
            cmd.Parameters.AddWithValue("@hora", horaRiego);
            cmd.Parameters.AddWithValue("@notas", notas ?? "");
            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        // RF1: Eliminar horario de riego
        [HttpPost]
        public async Task<IActionResult> EliminarRiego(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("DELETE FROM CalendarioRiego WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        // RF3: Emparejar/Conectar sensor Wi-Fi o Bluetooth
        [HttpPost]
        public async Task<IActionResult> ConectarSensor(string nombre, string tipoConexion, string macOIp)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = "INSERT INTO Sensores (Nombre, TipoConexion, MacOIp, Conectado, UltimaConexion) VALUES (@nombre, @tipo, @mac, 1, NOW())";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nombre", nombre);
            cmd.Parameters.AddWithValue("@tipo", tipoConexion);
            cmd.Parameters.AddWithValue("@mac", macOIp);
            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        // RF5: Guardar frecuencia de registro histórico seleccionada por el usuario
        [HttpPost]
        public async Task<IActionResult> ActualizarFrecuencia(int frecuenciaMinutos)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("UPDATE ConfiguracionSistema SET FrecuenciaMinutos = @freq WHERE Id = 1", conn);
            cmd.Parameters.AddWithValue("@freq", frecuenciaMinutos);
            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        // RF4, RF5 & RF7: Recibir lectura del sensor ESP32 (Wokwi) y almacenar en MySQL
        [AllowAnonymous]
        [HttpPost]
        [Route("api/iot/lectura")]
        public async Task<IActionResult> RecibirLectura([FromBody] LecturaAmbiental datos, [FromQuery] bool forzar = false)
        {
            bool fueraDeRango = (datos.Temperatura < 18.0 || datos.Temperatura > 24.0) ||
                                (datos.Humedad < 75.0 || datos.Humedad > 85.0);

            int sensorId = datos.SensorId == 0 ? 1 : datos.SensorId;

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Actualizar marca de conexión del sensor
            using (var cmdSensor = new MySqlCommand("UPDATE Sensores SET Conectado = 1, UltimaConexion = NOW() WHERE Id = @id", conn))
            {
                cmdSensor.Parameters.AddWithValue("@id", sensorId);
                await cmdSensor.ExecuteNonQueryAsync();
            }

            // Consultar frecuencia configurada y última fecha guardada
            int freqMin = 60;
            using (var cmdFreq = new MySqlCommand("SELECT FrecuenciaMinutos FROM ConfiguracionSistema WHERE Id = 1", conn))
            {
                var res = await cmdFreq.ExecuteScalarAsync();
                if (res != null) freqMin = Convert.ToInt32(res);
            }

            DateTime? ultimaFecha = null;
            using (var cmdLast = new MySqlCommand("SELECT FechaRegistro FROM LecturasAmbientales ORDER BY FechaRegistro DESC, Id DESC LIMIT 1", conn))
            {
                var res = await cmdLast.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value) ultimaFecha = Convert.ToDateTime(res);
            }

            // Guardar si es simulación manual (forzar), si hay alerta fuera de rango, o si ya transcurrió la frecuencia elegida
            bool correspondeGuardar = forzar || fueraDeRango || ultimaFecha == null ||
                                      (DateTime.Now - ultimaFecha.Value).TotalMinutes >= freqMin;

            if (correspondeGuardar)
            {
                var sql = @"INSERT INTO LecturasAmbientales (SensorId, Temperatura, Humedad, FechaRegistro, FueraDeRango) 
                            VALUES (@sensorId, @temp, @hum, NOW(), @alerta)";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sensorId", sensorId);
                cmd.Parameters.AddWithValue("@temp", datos.Temperatura);
                cmd.Parameters.AddWithValue("@hum", datos.Humedad);
                cmd.Parameters.AddWithValue("@alerta", fueraDeRango);
                await cmd.ExecuteNonQueryAsync();
            }

            return Ok(new { ok = true, fueraDeRango, guardadoEnBd = correspondeGuardar });
        }

        // RF2 & RF8: Monitoreo en tiempo real
        [HttpGet]
        [Route("api/iot/monitoreo")]
        public async Task<IActionResult> MonitoreoEnVivo()
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            LecturaAmbiental? lectura = null;
            using (var cmd = new MySqlCommand("SELECT * FROM LecturasAmbientales ORDER BY FechaRegistro DESC, Id DESC LIMIT 1", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    lectura = new LecturaAmbiental
                    {
                        Id = reader.GetInt32("Id"),
                        Temperatura = reader.GetDouble("Temperatura"),
                        Humedad = reader.GetDouble("Humedad"),
                        FechaRegistro = reader.GetDateTime("FechaRegistro"),
                        FueraDeRango = reader.GetBoolean("FueraDeRango")
                    };
                }
            }

            var ahora = DateTime.Now;
            bool esHoraDeRegar = false;
            string mensajeRiego = "";

            using (var cmdRiego = new MySqlCommand("SELECT HoraRiego, Notas FROM CalendarioRiego WHERE Activo = 1", conn))
            using (var reader = await cmdRiego.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var hora = reader.GetTimeSpan("HoraRiego");
                    if (hora.Hours == ahora.Hour && hora.Minutes == ahora.Minute)
                    {
                        esHoraDeRegar = true;
                        mensajeRiego = reader.GetString("Notas");
                        break;
                    }
                }
            }

            var estadosSensores = new List<object>();
            using (var cmdSen = new MySqlCommand("SELECT Id, TIMESTAMPDIFF(SECOND, UltimaConexion, NOW()) AS SegundosInactivo FROM Sensores", conn))
            using (var readerSen = await cmdSen.ExecuteReaderAsync())
            {
                while (await readerSen.ReadAsync())
                {
                    int seg = readerSen.IsDBNull(1) ? 9999 : readerSen.GetInt32(1);
                    estadosSensores.Add(new { id = readerSen.GetInt32(0), enLinea = seg <= 20 });
                }
            }

            return Ok(new { lectura, esHoraDeRegar, mensajeRiego, estadosSensores });
        }
    }
}