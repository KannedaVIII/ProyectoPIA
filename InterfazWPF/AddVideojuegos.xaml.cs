using System;
using System.Net.Http; // <--- NECESARIO PARA CONECTAR
using System.Text;
using System.Text.Json; // <--- NECESARIO PARA JSON
using System.Windows;
using Backend;

namespace InterfazWPF
{
    public partial class AddVideojuegos : Window
    {
        // URL de tu API (Asegúrate de que el puerto 5022 es el correcto, igual que en MainWindow)
        private const string UrlApi = "http://localhost:5022/api/Videojuegos";

        public Videojuegos NuevoJuego { get; private set; }

        public AddVideojuegos()
        {
            InitializeComponent();
        }

        // --- BOTÓN AÑADIR (ACEPTAR) ---
        // IMPORTANTE: Ahora el método debe ser 'async' para poder esperar a la API
        private async void btnAceptar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtEquipo.Text))
            {
                MessageBox.Show("El Título y el Equipo son obligatorios.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Convertir Rating
            if (!decimal.TryParse(txtRating.Text.Replace(".", ","), out decimal rating))
            {
                rating = 0;
            }

            // 3. Crear el objeto Videojuego
            NuevoJuego = new Videojuegos(
                title: txtTitulo.Text,
                releaseDate: txtFecha.Text,
                team: txtEquipo.Text,
                rating: rating,
                timesListed: "0",
                numberReviews: "0",
                genres: $"['{txtGenero.Text}']",
                summary: "Sin resumen",
                review: "Sin review"
            );

            // --- 4. ENVIAR A LA BASE DE DATOS (LA PARTE QUE FALTABA) ---
            try
            {
                // Configuramos el cliente para ignorar errores de certificado (Localhost)
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using (var client = new HttpClient(handler))
                {
                    // Convertimos el juego a JSON
                    string json = JsonSerializer.Serialize(NuevoJuego);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Hacemos la petición POST
                    HttpResponseMessage response = await client.PostAsync(UrlApi, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("¡Juego guardado en la base de datos!");

                        // 5. Cerramos la ventana con ÉXITO
                        DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        string errorMsg = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error al guardar: {response.StatusCode}\n{errorMsg}", "Error API");
                        // NO cerramos la ventana para que el usuario pueda corregir y reintentar
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error Crítico");
            }
        }

        // --- BOTÓN CANCELAR ---
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}