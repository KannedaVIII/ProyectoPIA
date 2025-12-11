using System;
using System.Net.Http; // Para hacer peticiones web
using System.Text;
using System.Text.Json; // Para manejar el formato JSON
using System.Windows;
using System.Windows.Input;

namespace InterfazWPF
{
    public partial class Register : Window
    {
        // Cliente HTTP estático para reutilizar la conexión
        private static readonly HttpClient client = new HttpClient();

        public Register()
        {
            InitializeComponent();
            btnRegister.Click += BtnRegister_Click;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Recoger los datos de la interfaz
            string usuarioInput = txtUsername.Text.Trim();
            string emailInput = txtEmail.Text.Trim();
            string passInput = pbPassword.Password;
            string passConfirm = pbConfirmPassword.Password;

            // 2. Validaciones locales (antes de molestar al servidor)
            if (string.IsNullOrWhiteSpace(usuarioInput) || string.IsNullOrWhiteSpace(emailInput) || string.IsNullOrWhiteSpace(passInput))
            {
                MessageBox.Show("Por favor, rellena todos los campos.", "Campos Vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (passInput != passConfirm)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. PREPARAR LOS DATOS (Igual que tu clase 'User' del backend)
            // Es CRUCIAL que los nombres (Username, Password, Email) sean idénticos a tu API.
            var nuevoUsuario = new
            {
                Username = usuarioInput,
                Password = passInput,
                Email = emailInput
            };

            // Convertir a JSON
            string jsonString = JsonSerializer.Serialize(nuevoUsuario);
            var contenidoHttp = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // 4. CONFIGURAR LA URL
            // ¡OJO! Cambia el puerto '7123' por el que use tu API al iniciarse.
            string urlApi = "http://localhost:5022/api/Auth/register";

            try
            {
                // Enviamos los datos (POST) y esperamos (await)
                HttpResponseMessage respuesta = await client.PostAsync(urlApi, contenidoHttp);

                // 5. PROCESAR RESPUESTA
                if (respuesta.IsSuccessStatusCode)
                {
                    MessageBox.Show("¡Usuario registrado con éxito!", "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);
                    IrAlLogin();
                }
                else
                {
                    // Si falla (ej. error 400 porque el usuario existe)
                    string errorServidor = await respuesta.Content.ReadAsStringAsync();
                    MessageBox.Show($"No se pudo registrar: {errorServidor}", "Error del Servidor", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se puede conectar con el servidor.\nVerifica que la API esté encendida.\n\nDetalle: {ex.Message}", "Error de Conexión");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            IrAlLogin();
        }

        private void IrAlLogin()
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }
    }
}