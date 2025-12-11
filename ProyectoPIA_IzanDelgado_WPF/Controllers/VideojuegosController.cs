using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ProyectoPIA_IzanDelgado_WPF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
        public class VideojuegosController 
        {
            private readonly IRepository _repository;

            // Recibe el repositorio (Memoria o MySQL) a través de la interfaz
            public VideojuegosController(IRepository repository)
            {
                _repository = repository;
            }

            // Método que expone la funcionalidad a la 'Vista'
            // Simplemente delega en el repositorio, pero en un caso real
            // podría incluir lógica de validación o negocio.

            public List<Videojuegos> GetAllVideojuegos()
            {
                return _repository.GetAll();
            }

            public Videojuegos GetVideojuegoByTitle(string title)
            {
                return _repository.GetByTitle(title);
            }

            public void InsertVideojuego(Videojuegos nuevo)
            {
                _repository.Insert(nuevo);
            }

            public void UpdateVideojuego(Videojuegos actualizado)
            {
                _repository.Update(actualizado);
            }

            public void DeleteVideojuego(Videojuegos juego)
            {
                _repository.Delete(juego);
            }

            // Método para precargar datos iniciales
            public void CargarDatosIniciales(List<Videojuegos> lista)
            {
                _repository.CargarDesdeLista(lista);
            }
        }
    }

