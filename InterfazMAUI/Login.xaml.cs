using System.Text;
using System.Text.Json;

namespace InterfazMAUI;

public partial class Login : ContentPage
{
    private static readonly HttpClient client = new HttpClient();

    public Login()
    {
        InitializeComponent();
    }

    private async void BtnLogin_Clicked(object sender, EventArgs e)
    {
        string usuario = txtUsuarioLogin.Text?.Trim();
        string pass = txtPasswordLogin.Text;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Error", "Introduce usuario y contraseña", "OK");
            return;
        }

        // Activar carga
        loadingIndicator.IsVisible = true;
        loadingIndicator.IsRunning = true;
        btnLogin.IsEnabled = false;

        // Crear objeto para enviar
        var loginData = new
        {
            Username = usuario,
            Password = pass
        };

        string json = JsonSerializer.Serialize(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // URL
        string urlApi = (DeviceInfo.Platform == DevicePlatform.Android)
            ? "http://10.0.2.2:5022/api/Auth/login"
            : "http://localhost:5022/api/Auth/login";

        try
        {
            HttpResponseMessage response = await client.PostAsync(urlApi, content);

            if (response.IsSuccessStatusCode)
            {
                // --- CAMBIO: GUARDAMOS EL USUARIO EN LA VARIABLE GLOBAL ---
                App.UsuarioActual = usuario;

                // CAMBIAR LA PANTALLA PRINCIPAL
                // Si usas AppShell:
                Application.Current.MainPage = new AppShell();

                // Si NO usas AppShell y usas navegación normal, sería:
                // Application.Current.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                await DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo conectar: {ex.Message}", "OK");
        }
        finally
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            btnLogin.IsEnabled = true;
        }
    }

    // Ir a la pantalla de Registro
    private async void TapGestureRecognizer_GoToRegister(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new Register());
    }
}