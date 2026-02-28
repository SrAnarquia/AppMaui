using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace AppScanner;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public MainPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.Camera>();
    }

    private async void OnCaptureAndSend(object sender, EventArgs e)
    {
        try
        {
            ShowLoader(true);

            var stream = await cameraView.CaptureImage(CancellationToken.None);
            if (stream == null)
            {
                await ShowMessage("No se pudo capturar imagen", "#ff0033");
                return;
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // importante resetear posición

            // Crear contenido multipart/form-data
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(memoryStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            content.Add(fileContent, "image", "qr.jpg"); // "image" debe coincidir con el parámetro IFormFile

            // Enviar a la API
            var response = await _httpClient.PostAsync("https://192.168.2.211:7203/api/lector/imagen", content);
            var mensaje = await ObtenerMensajeDeRespuesta(response);

            if (response.IsSuccessStatusCode)
                await ShowMessage("✔ " + mensaje, "#1e1e2f");
            else
                await ShowMessage("⚠ " + mensaje, "#ff0033");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error: " + ex.Message, "#ff0033");
        }
        finally
        {
            ShowLoader(false);
        }
    }

    private void ShowLoader(bool visible)
    {
        loader.IsVisible = visible;
        loader.IsRunning = visible;
        overlay.IsVisible = visible;
    }

    private async Task ShowMessage(string message, string color)
    {
        responseLabel.Text = message;
        responseFrame.BackgroundColor = Color.FromArgb(color);

        responseFrame.IsVisible = true;
        responseFrame.Opacity = 0;

        await responseFrame.FadeTo(1, 250);
        await Task.Delay(2500);
        await responseFrame.FadeTo(0, 500);

        responseFrame.IsVisible = false;
    }


    /*Manejo de respuestas*/

    private async Task<string> ObtenerMensajeDeRespuesta(HttpResponseMessage response)
    {
        if (response == null)
            return "Respuesta nula del servidor";

        string contenido = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(contenido))
            return "Respuesta vacía del servidor";

        try
        {
            using var doc = JsonDocument.Parse(contenido);
            if (doc.RootElement.TryGetProperty("mensaje", out var mensajeElement))
            {
                return mensajeElement.GetString() ?? "Sin mensaje";
            }
            else
            {
                // Si no tiene la propiedad "mensaje", devolvemos el contenido completo
                return contenido;
            }
        }
        catch
        {
            // Si no es JSON válido, devolvemos el contenido tal cual
            return contenido;
        }
    }


    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        try
        {
            var hoy = DateTime.Today.ToString("yyyy-MM-dd");

            var url = $"https://192.168.2.211:7203/api/lector/historial?fecha={hoy}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await ShowMessage("No se pudo obtener el historial", "#ff0033");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            await Navigation.PushAsync(new HistorialPage(json));
        }
        catch (Exception ex)
        {
            await ShowMessage(ex.Message, "#ff0033");
        }
    }



}


/*using System.Text;
using System.Text.Json;

namespace AppScanner;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public MainPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.Camera>();
    }

    private async void OnCaptureAndSend(object sender, EventArgs e)
    {
        try
        {
            loader.IsVisible = true;
            loader.IsRunning = true;

            var stream = await cameraView.CaptureImage(CancellationToken.None);

            if (stream == null)
            {
                ShowMessage("No se pudo capturar imagen", "#ff0033");
                return;
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var payload = new
            {
                imagenQr = base64Image
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://localhost:7203/api/lector/imagen",
                content);

            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                ShowMessage("✔ " + responseText, "#1e1e2f");
            }
            else
            {
                ShowMessage("⚠ " + responseText, "#ff0033");
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error: " + ex.Message, "#ff0033");
        }
        finally
        {
            loader.IsRunning = false;
            loader.IsVisible = false;
        }
    }

    private async void ShowMessage(string message, string color)
    {
        responseLabel.Text = message;
        responseFrame.BackgroundColor = Color.FromArgb(color);

        responseFrame.IsVisible = true;
        responseFrame.Opacity = 0;

        await responseFrame.FadeTo(1, 250);
        await Task.Delay(2500);
        await responseFrame.FadeTo(0, 500);

        responseFrame.IsVisible = false;
    }
}
*/