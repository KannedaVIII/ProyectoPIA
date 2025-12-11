using System.Collections.Generic;
using System.Linq;
namespace Backend.Models
{
    public class BasedeDatosUsuarios
    {
        public static List<User> ListaUsuarios = new List<User>();

        public static bool GuardarUsuario(User nuevoUsuario)
        {
            // Validamos si ya existe el usuario
            if (ListaUsuarios.Any(u => u.Username == nuevoUsuario.Username))
            {
                return false; // El usuario ya existe
            }

            ListaUsuarios.Add(nuevoUsuario);
            return true; // Guardado con éxito
        }

        // Método extra para validar el login más tarde
        public static bool ValidarLogin(string usuario, string pass)
        {
            return ListaUsuarios.Any(u => (u.Username == usuario || u.Email == usuario) && u.Password == pass);
        }
    }
}