using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InterfazMAUI.Models;
// using Backend; // Si lo necesitas

namespace InterfazMAUI;

public partial class MainPage : ContentPage
{
    // Colección para la interfaz
    public ObservableCollection<Videojuegos> ListaJuegos { get; set; }
    private List<Videojuegos> _todosLosJuegos; // Respaldo para el buscador local

    // Cliente HTTP
    private static readonly HttpClient client = new HttpClient();

    // URL Base
    private string UrlApi;

    public MainPage()
    {
        InitializeComponent();
        ConfigurarInicio();
    }

    private void ConfigurarInicio()
    {
        // 1. Configurar URL según dispositivo
        string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        UrlApi = $"http://{baseUrl}:5022/api/Videojuegos";

        // 2. Inicializar listas
        ListaJuegos = new ObservableCollection<Videojuegos>();

        // Enlazamos la lista al CollectionView (cvJuegos debe existir en XAML)
        cvJuegos.ItemsSource = ListaJuegos;
        _todosLosJuegos = new List<Videojuegos>();

        // 3. Token (Si aplica)
        // if (!string.IsNullOrEmpty(App.TokenActual)) ...

        // --- CAMBIO: ASIGNAR NOMBRE DE USUARIO ---
        // Buscamos la etiqueta "lblNombreUsuario" que definimos en el XAML
        if (lblNombreUsuario != null)
        {
            if (!string.IsNullOrEmpty(App.UsuarioActual))
            {
                lblNombreUsuario.Text = App.UsuarioActual;
            }
            else
            {
                lblNombreUsuario.Text = "Invitado";
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarJuegosDesdeApi();
    }

    // --- 1. MOSTRAR DATOS (GET) ---
    private async void OnSincronizarClicked(object sender, EventArgs e)
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
            }
            else
            {
                await DisplayAlert("Error API", $"Código: {respuesta.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Conexión", $"Verifica que la API esté corriendo.\n{ex.Message}", "OK");
        }
    }

    // --- 2. AÑADIR JUEGO ---
    private async void OnAddGameClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddJuego());
    }

    // --- 3. ELIMINAR JUEGO ---
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (cvJuegos.SelectedItem is Videojuegos juegoSeleccionado)
        {
            bool confirm = await DisplayAlert("Confirmar", $"¿Eliminar '{juegoSeleccionado.Title}'?", "Sí", "No");
            if (!confirm) return;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, UrlApi);
                string json = JsonSerializer.Serialize(juegoSeleccionado);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var respuesta = await client.SendAsync(request);

                if (respuesta.IsSuccessStatusCode)
                {
                    ListaJuegos.Remove(juegoSeleccionado);
                    _todosLosJuegos.Remove(juegoSeleccionado);
                    await DisplayAlert("Éxito", "Juego eliminado.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar en el servidor.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
        else
        {
            await DisplayAlert("Aviso", "Selecciona un juego de la lista primero.", "OK");
        }
    }

    // --- 4. MODIFICAR JUEGO ---
    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (cvJuegos.SelectedItem is Videojuegos juegoSeleccionado)
        {
            await Navigation.PushAsync(new ModJuego(juegoSeleccionado));
            cvJuegos.SelectedItem = null;
        }
        else
        {
            await DisplayAlert("Aviso", "Selecciona un juego para modificar.", "OK");
        }
    }

    // --- 5. MIGRAR CSV ---
    private async void OnMigrarCsvClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Migración", "Se leerá 'games.csv' y se subirá a MySQL. ¿Continuar?", "Sí", "No");
        if (!confirm) return;

        try
        {
            var listaParaSubir = new List<Videojuegos>();

            using var stream = await FileSystem.OpenAppPackageFileAsync("games.csv");
            using var reader = new StreamReader(stream);

            string header = await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                string linea = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(linea)) continue;

                var datos = ParsearLineaCSV(linea);

                if (datos.Count >= 8)
                {
                    decimal.TryParse(datos[4], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rating);

                    var nuevoJuego = new Videojuegos
                    {
                        Title = datos[1],
                        ReleaseDate = datos[2],
                        Team = datos[3],
                        Rating = rating,
                        TimesListed = datos[5],
                        NumberReviews = datos[6],
                        Genres = datos[7],
                        Summary = datos.Count > 8 ? datos[8] : "Sin resumen",
                        Review = datos.Count > 9 ? datos[9] : "Sin reseñas"
                    };

                    listaParaSubir.Add(nuevoJuego);
                }
            }

            string urlSeed = $"{UrlApi}/seed";
            string json = JsonSerializer.Serialize(listaParaSubir);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var respuesta = await client.PostAsync(urlSeed, content);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Finalizado", $"¡Éxito! Juegos subidos.", "OK");
                await CargarJuegosDesdeApi();
            }
            else
            {
                string err = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error API", err, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Crítico", $"Error: {ex.Message}", "OK");
        }
    }

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

    // --- 6. BUSCADOR ---
    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_todosLosJuegos == null) return;
        string filtro = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(filtro))
        {
            cvJuegos.ItemsSource = new ObservableCollection<Videojuegos>(_todosLosJuegos);
        }
        else
        {
            var filtrados = _todosLosJuegos.Where(j => j.Title != null && j.Title.ToLower().Contains(filtro)).ToList();
            cvJuegos.ItemsSource = new ObservableCollection<Videojuegos>(filtrados);
        }
    }

    // --- 7. LOGOUT ---
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Salir", "¿Cerrar sesión?", "Sí", "No");
        if (confirm)
        {
            // --- CAMBIO: LIMPIAR EL USUARIO ACTUAL ---
            App.UsuarioActual = null;
            // App.TokenActual = null; // Si usas tokens

            // Volver al Login
            Application.Current.MainPage = new NavigationPage(new Login());
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Lógica opcional
    }
    // --- 8. ABRIR ASISTENTE IA ---
    private async void OnAbrirAsistenteClicked(object sender, EventArgs e)
    {
        // Navegamos a la página del asistente que creamos anteriormente
        await Navigation.PushAsync(new AsistenteIA());
    }
}