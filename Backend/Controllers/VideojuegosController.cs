using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Backend
{
    [Route("api/[controller]")]
    [ApiController]
    // 1. Heredar de ControllerBase es esencial para una API
    public class VideojuegosController : ControllerBase
    {
        private readonly IRepository _repository;

        public VideojuegosController(IRepository repository)
        {
            _repository = repository;
        }

        // GET: api/Videojuegos
        [HttpGet]
        public ActionResult<List<Videojuegos>> GetAllVideojuegos()
        {
            // Envolvemos la respuesta en un ActionResult
            return Ok(_repository.GetAll());
        }

        // GET: api/Videojuegos/SuperMario
        // 2. Agregamos "{title}" a la ruta para diferenciarlo del GetAll
        [HttpGet("{title}")]
        public ActionResult<Videojuegos> GetVideojuegoByTitle(string title)
        {
            var juego = _repository.GetByTitle(title);

            // 3. Validamos si existe (igual que en tu ejemplo de Libros)
            if (juego == null)
            {
                return NotFound($"No se encontró el videojuego con título: {title}");
            }

            return Ok(juego);
        }

        // POST: api/Videojuegos
        // 4. Para insertar se usa POST, no PUT
        [HttpPost]
        public IActionResult InsertVideojuego([FromBody] Videojuegos nuevo)
        {
            if (nuevo == null)
            {
                return BadRequest("El videojuego no puede ser nulo.");
            }

            _repository.Insert(nuevo);

            // Retornamos 201 Created
            return CreatedAtAction(nameof(GetVideojuegoByTitle), new { title = nuevo.Title }, nuevo);
        }

        // PUT: api/Videojuegos
        // 5. Corregido [Http] por [HttpPut]
        [HttpPut]
        public IActionResult UpdateVideojuego([FromBody] Videojuegos actualizado)
        {
            // Aquí idealmente verificarías si el juego existe antes de actualizar
            // asumiendo que el repositorio maneja eso o lanza excepción:

            _repository.Update(actualizado);
            return NoContent(); // 204 No Content es estándar para updates
        }

        // DELETE: api/Videojuegos
        // 6. Agregado [HttpDelete]. 
        // Nota: Normalmente se borra por ID en la URL, pero para respetar tu firma
        // que recibe un objeto completo, usamos [FromBody].
        [HttpDelete]
        public IActionResult DeleteVideojuego([FromBody] Videojuegos juego)
        {
            _repository.Delete(juego);
            return NoContent();
        }

        // 7. Endpoint especial para carga masiva (Seed)
        [HttpPost("seed")]
        public IActionResult CargarDatosIniciales([FromBody] List<Videojuegos> lista)
        {
            _repository.CargarDesdeLista(lista);
            return Ok("Datos cargados correctamente.");
        }
    }
}