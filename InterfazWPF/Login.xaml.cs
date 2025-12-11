using System;
using System.Net.Http; // Para hablar con la API
using System.Text;
using System.Text.Json; // Para manejar JSON
using System.Windows;
using System.Windows.Navigation;

namespace InterfazWPF
{
    public partial class Login : Window
    {
        // Creamos el cliente HTTP una sola vez (static) para ahorrar recursos
        private static readonly HttpClient client = new HttpClient();

        public Login()
        {
            InitializeComponent();
            btnLogin.Click += BtnLogin_Click;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuarioInput = txtUser.Text.Trim();
            string passwordInput = pbPassword.Password;

            // 1. Validaciones básicas
            if (string.IsNullOrWhiteSpace(usuarioInput) || string.IsNullOrWhiteSpace(passwordInput))
            {
                MessageBox.Show("Por favor, introduce usuario y contraseña.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. PREPARAR DATOS PARA LA API
            // Creamos un objeto que coincida con la clase 'User' de tu Backend
            var loginData = new
            {
                Username = usuarioInput,
                Password = passwordInput,
                Email = "" // La API espera un objeto User completo, aunque el email vaya vacío en el login
            };

            // Convertimos a JSON: {"Username":"...", "Password":"...", "Email":""}
            string jsonString = JsonSerializer.Serialize(loginData);
            var contenidoHttp = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // 3. URL DE LA API
            // IMPORTANTE: Verifica que el puerto (7123) sea el mismo que en tu launchSettings.json
            string urlApi = "http://localhost:5022/api/Auth/login";

            try
            {
                // Enviamos la petición POST y esperamos (await)
                HttpResponseMessage respuesta = await client.PostAsync(urlApi, contenidoHttp);

                // Leemos lo que nos contestó la API
                string respuestaTexto = await respuesta.Content.ReadAsStringAsync();

                if (respuesta.IsSuccessStatusCode)
                {
                    // --- LOGIN EXITOSO ---

                    // 4. DESERIALIZAR EL TOKEN
                    // Tu API devuelve: { "token": "eyJhb...", "message": "Login exitoso" }
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var resultado = JsonSerializer.Deserialize<LoginRespuesta>(respuestaTexto, opciones);

                    // Guardamos el token en una variable global (Ver paso extra abajo)
                    App.TokenActual = resultado.Token;
                    App.UsuarioActual = usuarioInput; // Opcional: guardar el nombre

                    MessageBox.Show("¡Login Correcto!", "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 5. ABRIR VENTANA PRINCIPAL
                    // Nota: Si tu constructor de MainWindow espera un string, úsalo. 
                    // Si no, usa el constructor vacío.

                    // Opción A: Si MainWindow no recibe parámetros
                    MainWindow dashboard = new MainWindow();

                    // Opción B: Si MainWindow recibe el usuario (como en tu código original)
                    // MainWindow dashboard = new MainWindow(usuarioInput);

                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    // --- ERROR (401 Unauthorized) ---
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Acceso", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar con el servidor.\n¿Está encendida la API?\n\nError: {ex.Message}", "Error de Conexión");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Register registerWindow = new Register();
            registerWindow.Show();
            this.Close();
        }

        // Clase auxiliar para leer la respuesta JSON de tu API
        public class LoginRespuesta
        {
            public string Token { get; set; }
            public string Message { get; set; }
        }
    }
}