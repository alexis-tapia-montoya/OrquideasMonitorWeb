using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OrquideasMonitorWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connStr;

        public AccountController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Invernadero");

            return View();
        }

        // Login tradicional con Usuario y Contraseña
        [HttpPost]
        public async Task<IActionResult> LoginCredenciales(string usuario, string password)
        {
            string hash = HashSha256(password ?? "");

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "SELECT Id, NombreCompleto, Usuario FROM Usuarios WHERE Usuario = @u AND PasswordHash = @p AND Activo = TRUE LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@u", usuario);
            cmd.Parameters.AddWithValue("@p", hash);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                await IniciarCookieAsync(reader.GetInt32("Id"), reader.GetString("NombreCompleto"), reader.GetString("Usuario"));
                return RedirectToAction("Index", "Invernadero");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View("Login");
        }

        // Endpoint para entregar los descriptores faciales registrados a face-api.js
        [HttpGet("/api/auth/usuarios-rostros")]
        public async Task<IActionResult> ObtenerRostros()
        {
            var lista = new List<object>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "SELECT Id, NombreCompleto, DescriptorFacial FROM Usuarios WHERE Activo = TRUE AND DescriptorFacial IS NOT NULL", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    id = reader.GetInt32("Id"),
                    nombreCompleto = reader.GetString("NombreCompleto"),
                    descriptorFacial = reader.GetString("DescriptorFacial")
                });
            }
            return Ok(lista);
        }

        // Endpoint llamado por face-api.js al reconocer el rostro
        [HttpPost("/api/auth/login-facial")]
        public async Task<IActionResult> LoginFacial([FromBody] LoginFacialDto dto)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "SELECT Id, NombreCompleto, Usuario FROM Usuarios WHERE Id = @id AND Activo = TRUE LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", dto.UsuarioId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                await IniciarCookieAsync(reader.GetInt32("Id"), reader.GetString("NombreCompleto"), reader.GetString("Usuario"));
                return Ok(new { exito = true, redirectUrl = Url.Action("Index", "Invernadero") });
            }
            return Unauthorized(new { exito = false });
        }

        // Endpoint para registrar/actualizar el rostro de un usuario existente
        [HttpPost("/api/auth/registrar-rostro")]
        public async Task<IActionResult> RegistrarRostro([FromBody] RegistroRostroDto dto)
        {
            string hash = HashSha256(dto.Password ?? "");

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "UPDATE Usuarios SET DescriptorFacial = @desc WHERE Usuario = @u AND PasswordHash = @p AND Activo = TRUE", conn);
            cmd.Parameters.AddWithValue("@desc", dto.DescriptorFacial);
            cmd.Parameters.AddWithValue("@u", dto.Usuario);
            cmd.Parameters.AddWithValue("@p", hash);

            int filas = await cmd.ExecuteNonQueryAsync();
            if (filas > 0)
                return Ok(new { exito = true, mensaje = "Rostro vinculado correctamente. Ya puedes ingresar con cámara." });

            return BadRequest(new { exito = false, mensaje = "Credenciales inválidas. Verifica usuario y contraseña." });
        }

        // Solo el usuario "admin" puede crear nuevos usuarios
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CrearUsuario(string nombreCompleto, string usuario, string password)
        {
            if (User.FindFirst("Usuario")?.Value != "admin")
                return Forbid();

            if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorAdmin"] = "Todos los campos son obligatorios.";
                return RedirectToAction("Index", "Invernadero");
            }

            try
            {
                await using var conn = new MySqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new MySqlCommand(
                    "INSERT INTO Usuarios (NombreCompleto, Usuario, PasswordHash, Activo) VALUES (@nombre, @u, @p, TRUE)", conn);
                cmd.Parameters.AddWithValue("@nombre", nombreCompleto.Trim());
                cmd.Parameters.AddWithValue("@u", usuario.Trim());
                cmd.Parameters.AddWithValue("@p", HashSha256(password));
                await cmd.ExecuteNonQueryAsync();

                TempData["MensajeAdmin"] = $"Usuario '{usuario.Trim()}' creado. Ya puede ingresar y vincular su rostro.";
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                TempData["ErrorAdmin"] = $"El usuario '{usuario.Trim()}' ya existe.";
            }

            return RedirectToAction("Index", "Invernadero");
        }

        // Solo el usuario "admin" puede eliminar otros usuarios
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            if (User.FindFirst("Usuario")?.Value != "admin")
                return Forbid();

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("DELETE FROM Usuarios WHERE Id = @id AND Usuario <> 'admin'", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();

            TempData["MensajeAdmin"] = "Usuario eliminado correctamente.";
            return RedirectToAction("Index", "Invernadero");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task IniciarCookieAsync(int id, string nombre, string usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim("Usuario", usuario)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        private static string HashSha256(string texto)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    public class LoginFacialDto { public int UsuarioId { get; set; } }
    public class RegistroRostroDto
    {
        public string Usuario { get; set; } = "";
        public string Password { get; set; } = "";
        public string DescriptorFacial { get; set; } = "";
    }
}