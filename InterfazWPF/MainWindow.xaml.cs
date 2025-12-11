using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http; // Necesario para API
using System.Net.Http.Headers; // Necesario para Token
using System.Text;
using System.Text.Json; // Necesario para JSON
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Backend; // <--- REFERENCIA A TUS MODELOS

namespace InterfazWPF
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Videojuegos> ListaJuegos { get; set; }
        private List<Videojuegos> _todosLosJuegos; // Respaldo para el buscador

        // Cliente HTTP único
        // Cliente HTTP configurado para IGNORAR errores de certificado SSL (Solo para Localhost)
        private static readonly HttpClient client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        });

        // URL DE TU API (Verifica el puerto en launchSettings.json del Backend)
        private const string UrlApi = "http://localhost:5022/api/Videojuegos";

        public MainWindow()
        {
            InitializeComponent();
            ConfigurarInicio();
        }

        public MainWindow(string nombreUsuario)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(nombreUsuario)) App.UsuarioActual = nombreUsuario;
            ConfigurarInicio();
        }

        private void ConfigurarInicio()
        {
            // 1. Configurar Listas
            ListaJuegos = new ObservableCollection<Videojuegos>();
            lvVideojuegos.ItemsSource = ListaJuegos;
            _todosLosJuegos = new List<Videojuegos>();

            // 2. Poner nombre usuario
            if (txtNombreUsuario != null && !string.IsNullOrEmpty(App.UsuarioActual))
            {
                txtNombreUsuario.Text = App.UsuarioActual;
            }

            // 3. Configurar Token para peticiones
            if (!string.IsNullOrEmpty(App.TokenActual))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", App.TokenActual);
            }
        }

        // --- 1. MOSTRAR DATOS (GET) ---
        private async void btnMostrar_Click(object sender, RoutedEventArgs e)
        {
            await CargarJuegosDesdeApi();
        }

        private async Task CargarJuegosDesdeApi()
        {
            try
            {
                HttpResponseMessage respuesta = await client.GetAsync(UrlApi);
                if (respuesta.IsSuccessStatusCode)
                {
                    string json = await respuesta.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var juegosTraidos = JsonSerializer.Deserialize<List<Videojuegos>>(json, opciones);

                    ListaJuegos.Clear();
                    foreach (var juego in juegosTraidos) ListaJuegos.Add(juego);

                    _todosLosJuegos = new List<Videojuegos>(ListaJuegos);
                    MessageBox.Show($"Datos sincronizados. {ListaJuegos.Count} juegos cargados.", "Conexión OK");
                }
                else
                {
                    MessageBox.Show($"Error del servidor: {respuesta.StatusCode}", "Error API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar a la API. Verifica que el Backend esté corriendo.\n{ex.Message}", "Error Conexión");
            }
        }

        // --- 2. AÑADIR JUEGO (POST) ---
        private async void btnAnadir_Click(object sender, RoutedEventArgs e)
        {
            AddVideojuegos ventanaAnadir = new AddVideojuegos();
            bool? resultado = ventanaAnadir.ShowDialog();

            if (resultado == true)
            {
                // Si la ventana devuelve true, recargamos la lista completa para ver el nuevo dato
                await CargarJuegosDesdeApi();
            }
        }

        // --- 3. ELIMINAR JUEGO (DELETE) ---
        private async void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (lvVideojuegos.SelectedItem is Videojuegos juegoSeleccionado)
            {
                var confirm = MessageBox.Show($"¿Eliminar '{juegoSeleccionado.Title}' de la base de datos?",
                                              "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Construimos petición DELETE con Body (para enviar el objeto completo)
                        var request = new HttpRequestMessage(HttpMethod.Delete, UrlApi);
                        string json = JsonSerializer.Serialize(juegoSeleccionado);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                        var respuesta = await client.SendAsync(request);

                        if (respuesta.IsSuccessStatusCode)
                        {
                            ListaJuegos.Remove(juegoSeleccionado);
                            _todosLosJuegos.Remove(juegoSeleccionado);
                            MessageBox.Show("Juego eliminado.", "Éxito");
                        }
                        else
                        {
                            MessageBox.Show("Error al eliminar en servidor.", "Fallo API");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecciona un juego.", "Aviso");
            }
        }

        // --- 4. MODIFICAR JUEGO (PUT) ---
        private async void btnModificar_Click(object sender, RoutedEventArgs e)
        {
            if (lvVideojuegos.SelectedItem is Videojuegos juegoSeleccionado)
            {
                ModVideojuegos ventanaMod = new ModVideojuegos(juegoSeleccionado);
                bool? resultado = ventanaMod.ShowDialog();

                if (resultado == true)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(juegoSeleccionado);
                        var contenido = new StringContent(json, Encoding.UTF8, "application/json");

                        var respuesta = await client.PutAsync(UrlApi, contenido);

                        if (respuesta.IsSuccessStatusCode)
                        {
                            lvVideojuegos.Items.Refresh();
                            MessageBox.Show("Modificación guardada.", "Éxito");
                        }
                        else
                        {
                            MessageBox.Show("Error al guardar cambios.", "Fallo API");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecciona un juego.", "Aviso");
            }
        }

        // --- 5. MIGRAR CSV A MYSQL (SEED) ---
        private async void btnMigrar_Click(object sender, RoutedEventArgs e)
        {
            string rutaArchivo = "games.csv";
            if (!File.Exists(rutaArchivo))
            {
                MessageBox.Show("No encuentro 'games.csv' en la carpeta del ejecutable.", "Archivo no encontrado");
                return;
            }

            var confirm = MessageBox.Show("Esto leerá el CSV y subirá todos los juegos a MySQL.\n¿Continuar?", "Migración", MessageBoxButton.YesNo);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                var listaParaSubir = new List<Videojuegos>();
                var lineas = File.ReadAllLines(rutaArchivo);

                foreach (var linea in lineas.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;
                    var datos = ParsearLineaCSV(linea);

                    if (datos.Count >= 8)
                    {
                        decimal.TryParse(datos[4], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rating);
                        listaParaSubir.Add(new Videojuegos(
                            datos[1], datos[2], datos[3], rating, datos[5], datos[6], datos[7],
                            datos.Count > 8 ? datos[8] : "",
                            datos.Count > 9 ? datos[9] : ""
                        ));
                    }
                }

                // Enviar al endpoint /seed
                string urlSeed = $"{UrlApi}/seed";
                string json = JsonSerializer.Serialize(listaParaSubir);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var respuesta = await client.PostAsync(urlSeed, content);

                if (respuesta.IsSuccessStatusCode)
                {
                    MessageBox.Show($"¡Éxito! {listaParaSubir.Count} juegos subidos a MySQL.", "Finalizado");
                    await CargarJuegosDesdeApi();
                }
                else
                {
                    string err = await respuesta.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error API: {err}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico: {ex.Message}");
            }
        }

        // Parser manual del CSV
        private List<string> ParsearLineaCSV(string linea)
        {
            var valores = new List<string>();
            var valorActual = "";
            bool enComillas = false;
            bool enCorchetes = false;

            foreach (char c in linea)
            {
                if (c == '"') enComillas = !enComillas;
                else if (c == '[') { enCorchetes = true; valorActual += c; }
                else if (c == ']') { enCorchetes = false; valorActual += c; }
                else if (c == ',' && !enComillas && !enCorchetes)
                {
                    valores.Add(valorActual.Trim().Trim('"'));
                    valorActual = "";
                }
                else valorActual += c;
            }
            valores.Add(valorActual.Trim().Trim('"'));
            return valores;
        }

        // --- 6. BUSCADOR LOCAL ---
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_todosLosJuegos == null) return;
            string filtro = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
                lvVideojuegos.ItemsSource = _todosLosJuegos;
            else
                lvVideojuegos.ItemsSource = _todosLosJuegos.Where(j => j.Title.ToLower().Contains(filtro)).ToList();
        }

        // --- 7. CERRAR SESIÓN ---
        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resp = MessageBox.Show("¿Cerrar sesión?", "Salir", MessageBoxButton.YesNo);
            if (resp == MessageBoxResult.Yes)
            {
                App.UsuarioActual = "";
                App.TokenActual = "";
                client.DefaultRequestHeaders.Authorization = null;

                Login login = new Login();
                login.Show();
                this.Close();
            }
        }

        // --- 8. ABRIR ASISTENTE MCP (NUEVO) ---
        private void btnMCP_Click(object sender, RoutedEventArgs e)
        {
            // Instanciamos la nueva ventana del asistente MCP
            AsistenteMCP ventanaIA = new AsistenteMCP();
            // La mostramos de forma no modal para que el usuario pueda ver los datos mientras chatea
            ventanaIA.Show();
        }
    }
}