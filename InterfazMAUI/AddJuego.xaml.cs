using System.Text;
using System.Text.Json;
using System.Globalization;
using InterfazMAUI.Models;

namespace InterfazMAUI;

public partial class AddJuego : ContentPage // ¡Clase renombrada a AddJuego!
{
    private readonly HttpClient client = new HttpClient();
    private string UrlApi;

    public AddJuego()
    {
        InitializeComponent();
        ConfigurarUrl();
    }

    private void ConfigurarUrl()
    {
        // Ajustar URL según dispositivo
        string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        UrlApi = $"http://{baseUrl}:5022/api/Videojuegos";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // 1. Validaciones
        if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtEquipo.Text))
        {
            await DisplayAlert("Faltan datos", "El Título y el Equipo son obligatorios.", "OK");
            return;
        }

        // 2. Convertir Rating (Maneja tanto punto como coma decimal)
        string ratingTexto = txtRating.Text?.Replace(",", ".") ?? "0";
        if (!decimal.TryParse(ratingTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rating))
        {
            rating = 0;
        }

        // Bloquear interfaz
        loading.IsVisible = true;
        loading.IsRunning = true;
        ((Button)sender).IsEnabled = false;

        try
        {
            // 3. Crear el objeto Videojuego
            var nuevoJuego = new Videojuegos
            {
                Title = txtTitulo.Text,
                Genres = txtGenero.Text,
                Team = txtEquipo.Text,
                ReleaseDate = dpFecha.Date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                Rating = rating,

                Summary = "Sin resumen",      // O string.Empty
                Review = "Sin reseña",        // O string.Empty
                TimesListed = "null",              // Asumiendo que es int
                NumberReviews = "null",
            };

            // 4. Serializar a JSON
            string json = JsonSerializer.Serialize(nuevoJuego);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 5. Enviar POST
            var response = await client.PostAsync(UrlApi, content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Juego guardado correctamente.", "OK");

                // Volver a la pantalla anterior
                await Navigation.PopAsync();
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error API", $"No se pudo guardar: {error}", "OK");
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
            ((Button)sender).IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}