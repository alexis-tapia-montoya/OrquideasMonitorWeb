# OrquideasMonitorWeb — Monitor Climático de Orquídeas (ASP.NET Core MVC + ESP32 / Wokwi)

Aplicación web en **ASP.NET Core MVC** integrada con **MySQL** y **ESP32 (simulado en Wokwi)** para monitorear temperatura y humedad relativa en cultivos de orquídeas (parámetros de referencia: humedad cercana al **80%** [75%–85%] y temperatura entre **18 °C y 24 °C**), gestionar horarios de riego, consultar registros históricos y controlar el acceso mediante autenticación híbrida (credenciales + reconocimiento facial con `face-api.js`).

---

## 1. Requisitos y tecnologías

### Backend y aplicación web
* **Framework / Lenguaje:** .NET / ASP.NET Core MVC (C#)
* **Base de datos:** MySQL (compatible con instancias remotas como Clever Cloud o locales)
* **Paquete de acceso a datos:** `MySqlConnector`
* **Autenticación y seguridad:** ASP.NET Core Cookie Authentication (`AddCookie`), hash de contraseñas **SHA-256** y control de acceso (`[Authorize]`, `[AllowAnonymous]`, administración de usuarios exclusiva para `admin`)
* **Frontend / Librerías:** Bootstrap 5, Chart.js y `face-api.js` (umbral de coincidencia configurado en `0.65`)

### Hardware / Simulación IoT
* **Microcontrolador:** ESP32 (simulado en Wokwi bajo `Wokwi_ESP32`: `sketch.ino`, `diagram.json`, `libraries.txt`)
* **Sensor:** DHT22 (temperatura y humedad relativa)
* **Comunicación:** Wi-Fi (`Wokwi-GUEST` en simulación) mediante peticiones `HTTP POST` hacia `/api/iot/lectura` (con túnel HTTPS como Dev Tunnel cuando se envía desde Wokwi hacia el entorno local)

---

## 2. Estructura del proyecto

* `Controllers/`
  * `AccountController.cs`: inicio/cierre de sesión, verificación/vinculación biométrica (`face-api.js`) y gestión de usuarios para `admin`.
  * `InvernaderoController.cs`: panel principal (`Index`), riego, sensores, frecuencia de registro, ingesta de lecturas (`POST /api/iot/lectura`) y monitoreo en vivo (`GET /api/iot/monitoreo`).
* `Models/`: `LecturaAmbiental.cs`, `CalendarioRiego.cs`, `SensorDispositivo.cs`, `ErrorViewModel.cs`.
* `Views/`
  * `Account/Login.cshtml`: acceso con usuario/contraseña y reconocimiento facial.
  * `Invernadero/Index.cshtml`: tarjetas de estado, gráfico histórico, calendario de riego, estado de sensores, panel de usuarios (`admin`) y cierre automático por inactividad (3 minutos).
* `Wokwi_ESP32/`: `sketch.ino`, `diagram.json` y `libraries.txt`.
* `appsettings.example.json`: plantilla de configuración sin credenciales reales.

---

## 3. Configuración e instalación

### Paso 1: Base de datos MySQL
1. Crea o prepara tu base de datos MySQL.
2. Ejecuta el script SQL de base de datos en tu servidor o cliente SQL (por ejemplo, phpMyAdmin o MySQL Workbench) para crear las tablas (`Usuarios`, `Sensores`, `LecturasAmbientales`, `CalendarioRiego`, `ConfiguracionSistema`) y los registros iniciales.

### Paso 2: Configuración de conexión (`appsettings.json`)
1. Copia `appsettings.example.json` como `appsettings.json` en la raíz del proyecto (este archivo debe permanecer ignorado en Git para no exponer credenciales).
2. Completa `ConnectionStrings:DefaultConnection` con los datos reales de tu servidor MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_HOST_MYSQL;Port=3306;Database=TU_BASE_DE_DATOS;Uid=TU_USUARIO;Pwd=TU_CONTRASEÑA;SslMode=Preferred;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}