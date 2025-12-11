using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient; // Necesario para MySQL
using System.Data;

namespace Backend
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;

        // Inyectamos la configuración para poder leer la cadena de conexión del appsettings.json
        public AuthController(JwtService jwtService, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _configuration = configuration;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            string connectionString = _configuration.GetConnectionString("CadenaMySQL");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Verificar si el usuario ya existe
                    string checkQuery = "SELECT COUNT(*) FROM datosinicio WHERE usuario = @user";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@user", user.Username);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0) return BadRequest("El usuario ya existe.");
                    }

                    // 2. Insertar el nuevo usuario
                    // Asegúrate de que los nombres de las columnas (usuario, gmail, contrasena) coincidan con tu tabla en MySQL
                    string query = "INSERT INTO datosinicio (usuario, gmail, contrasena) VALUES (@user, @email, @pass)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user.Username);
                        cmd.Parameters.AddWithValue("@email", user.Email);
                        cmd.Parameters.AddWithValue("@pass", user.Password); // Nota: En producción, aquí deberías encriptar la contraseña

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Usuario registrado con éxito en MySQL" });
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginRequest)
        {
            string connectionString = _configuration.GetConnectionString("CadenaMySQL");
            User usuarioEncontrado = null;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Buscamos usuario y contraseña
                    string query = "SELECT usuario, gmail FROM datosinicio WHERE usuario = @user AND contrasena = @pass";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", loginRequest.Username);
                        cmd.Parameters.AddWithValue("@pass", loginRequest.Password);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Si entramos aquí, es que los datos son correctos
                                usuarioEncontrado = new User
                                {
                                    Username = reader["usuario"].ToString(),
                                    Email = reader["gmail"].ToString()
                                    // No necesitamos leer la contraseña de vuelta
                                };
                            }
                        }
                    }
                }

                if (usuarioEncontrado == null)
                {
                    return Unauthorized("Usuario o contraseña incorrectos.");
                }

                // Generamos el token
                var token = _jwtService.GenerateToken(usuarioEncontrado);

                return Ok(new
                {
                    token = token,
                    message = "Login exitoso desde MySQL"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}