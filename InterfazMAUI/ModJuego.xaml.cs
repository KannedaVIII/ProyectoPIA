using System.Globalization;
using System.Text;
using System.Text.Json;
using InterfazMAUI.Models;

namespace InterfazMAUI;

public partial class ModJuego : ContentPage
{
    private Videojuegos _juegoOriginal;
    private readonly HttpClient client = new HttpClient();
    private string UrlApi;

    // El constructor recibe el juego que seleccionaste en la lista
    public ModJuego(Videojuegos juegoAEditar)
    {
        InitializeComponent();
        _juegoOriginal = juegoAEditar;
        ConfigurarUrl();
        CargarDatos();
    }

    private void ConfigurarUrl()
    {
        // Misma lógica de IP que en el resto de la app
        string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        UrlApi = $"http://{baseUrl}:5022/api/Videojuegos";
    }

    private void CargarDatos()
    {
        // 1. Rellenar los campos con los datos actuales
        txtTitulo.Text = _juegoOriginal.Title;
        txtGenero.Text = _juegoOriginal.Genres;
        txtEquipo.Text = _juegoOriginal.Team;
        txtRating.Text = _juegoOriginal.Rating.ToString(CultureInfo.InvariantCulture);

        // 2. Intentar poner la fecha en el DatePicker
        if (DateTime.TryParse(_juegoOriginal.ReleaseDate, out DateTime fecha))
        {
            dpFecha.Date = fecha;
        }

        // 3. Bloquear el Título (Clave primaria lógica)
        // Ya está IsReadOnly=True en el XAML, pero por seguridad:
        txtTitulo.IsEnabled = false;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(txtEquipo.Text))
        {
            await DisplayAlert("Error", "El equipo es obligatorio.", "OK");
            return;
        }

        loading.IsVisible = true;
        loading.IsRunning = true;

        try
        {
            // --- ACTUALIZAR EL OBJETO EN MEMORIA ---
            _juegoOriginal.Genres = txtGenero.Text;
            _juegoOriginal.Team = txtEquipo.Text;

            // Convertir Fecha al formato que le gusta a tu API (Inglés)
            _juegoOriginal.ReleaseDate = dpFecha.Date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);

            // Parsear Rating (Usamos InvariantCulture para aceptar puntos como decimales)
            string ratingTexto = txtRating.Text?.Replace(",", ".") ?? "0";
            if (decimal.TryParse(ratingTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nuevoRating))
            {
                _juegoOriginal.Rating = nuevoRating;
            }
            else
            {
                _juegoOriginal.Rating = 0;
            }

            // Asegurar campos obligatorios que no están en el formulario (para evitar error 400)
            if (_juegoOriginal.TimesListed == null) _juegoOriginal.TimesListed = "null"; // OJO AQUÍ, debe ser int, no "null" string
            // Si en tu modelo original son strings con valor "null", déjalo como estaba. 
            // Pero idealmente deben ser números o strings vacíos.

            // --- ENVIAR EL PUT A LA API ---
            string json = JsonSerializer.Serialize(_juegoOriginal);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Nota: Asumiendo que tu API usa el Título o ID en la URL para el PUT
            // Ejemplo: PUT api/Videojuegos (con body) O PUT api/Videojuegos/NombreJuego
            // Si tu API espera el objeto completo en el body a la URL base, usa esto:
            var response = await client.PutAsync(UrlApi, content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Juego actualizado correctamente.", "OK");
                await Navigation.PopAsync(); // Cierra la ventana y vuelve a la lista
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error API", $"No se pudo actualizar: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Fallo de conexión: {ex.Message}", "OK");
        }
        finally
        {
            loading.IsRunning = false;
            loading.IsVisible = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}