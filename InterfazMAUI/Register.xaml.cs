using System.Text;
using System.Text.Json;

namespace InterfazMAUI;

public partial class Register : ContentPage
{
    // Cliente HTTP (Igual que en WPF)
    private static readonly HttpClient client = new HttpClient();

    public Register()
    {
        InitializeComponent();
    }

    private async void BtnRegister_Clicked(object sender, EventArgs e)
    {
        // 1. Recoger datos
        string usuarioInput = txtUsername.Text?.Trim();
        string emailInput = txtEmail.Text?.Trim();
        string passInput = txtPassword.Text;
        string passConfirm = txtConfirmPassword.Text;

        // 2. Validaciones locales
        if (string.IsNullOrWhiteSpace(usuarioInput) || string.IsNullOrWhiteSpace(emailInput) || string.IsNullOrWhiteSpace(passInput))
        {
            await DisplayAlert("Campos Vacíos", "Por favor, rellena todos los campos.", "OK");
            return;
        }

        if (passInput != passConfirm)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        // Mostrar indicador de carga
        loadingIndicator.IsVisible = true;
        loadingIndicator.IsRunning = true;
        btnRegister.IsEnabled = false;

        // 3. Preparar datos (Objeto anónimo igual que en tu WPF)
        var nuevoUsuario = new
        {
            Username = usuarioInput,
            Password = passInput,
            Email = emailInput
        };

        string jsonString = JsonSerializer.Serialize(nuevoUsuario);
        var contenidoHttp = new StringContent(jsonString, Encoding.UTF8, "application/json");

        // 4. CONFIGURAR URL (La parte TRUCOSA de MAUI)
        string urlApi = "";

        // Si estamos ejecutando en Android, localhost es 10.0.2.2
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            urlApi = "http://10.0.2.2:5022/api/Auth/register";
        }
        else
        {
            // En Windows o iOS Simulator local
            urlApi = "http://localhost:5022/api/Auth/register";
        }

        try
        {
            HttpResponseMessage respuesta = await client.PostAsync(urlApi, contenidoHttp);

            if (respuesta.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "¡Usuario registrado con éxito!", "OK");
                await IrAlLogin();
            }
            else
            {
                string errorServidor = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error del Servidor", $"No se pudo registrar: {errorServidor}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Conexión", $"No se puede conectar con la API.\n{ex.Message}", "OK");
        }
        finally
        {
            // Ocultar carga y reactivar botón
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            btnRegister.IsEnabled = true;
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await IrAlLogin();
    }

    private async Task IrAlLogin()
    {
        await Navigation.PushAsync(new Login());
    }
}