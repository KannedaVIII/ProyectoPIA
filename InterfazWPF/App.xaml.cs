using System.Windows;

namespace InterfazWPF
{
    public partial class App : Application
    {
        // Aquí guardaremos el token para usarlo en toda la aplicación
        public static string TokenActual { get; set; } = string.Empty;

        // Aquí guardamos el nombre del usuario logueado
        public static string UsuarioActual { get; set; } = string.Empty;
    }
}