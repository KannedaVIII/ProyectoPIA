using Microsoft.AspNetCore.Mvc;

namespace ProyectoPIA_IzanDelgado_WPF
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        // Simulamos una base de datos en memoria (se borra al reiniciar la app)
        private static List<User> usersDb = new List<User>();

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (usersDb.Any(u => u.Username == user.Username))
            {
                return BadRequest("El usuario ya existe.");
            }

            // Guardamos el usuario (En la vida real, aquí guardarías en SQL y hashearías la contraseña)
            usersDb.Add(user);

            return Ok(new { message = "Usuario registrado con éxito" });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginRequest)
        {
            // Buscamos el usuario en nuestra lista
            var user = usersDb.FirstOrDefault(u => u.Username == loginRequest.Username && u.Password == loginRequest.Password);

            if (user == null)
            {
                return Unauthorized("Usuario o contraseña incorrectos.");
            }

            // Si el usuario existe, generamos el token
            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token = token,
                message = "Login exitoso"
            });
        }

        // GET: api/Auth/users (Solo para que veas los usuarios registrados)
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            return Ok(usersDb);
        }
    }
}