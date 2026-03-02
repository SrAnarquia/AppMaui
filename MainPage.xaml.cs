using AppScanner.Services;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AppScanner;

public partial class MainPage : ContentPage
{

    #region Builder
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private readonly SoundService _soundService;

    private IAudioPlayer? _resultPlayer;

    private readonly object _resultLock = new();

    private IAudioPlayer? _preloadedShotPlayer;

    public MainPage(
    HttpClient httpClient,
    IAudioManager audioManager,
    SoundService soundService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _audioManager = audioManager;
        _soundService = soundService;
    }
    #endregion

    #region PermisosDeCamaras
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.Camera>();


        await PreloadCameraSound();
    }
    #endregion

    #region CapturarImagen
    private async void OnCaptureAndSend(object sender, EventArgs e)
    {
        try
        {

            ShowLoader(true);
            await PlaySound(SoundType.CameraShot);
            decimal? precioManual = null;

            if (chkPrecioManual.IsChecked)
            {
                if (!decimal.TryParse(txtPrecioManual.Text, out var precio) || precio <= 0)
                {
                    await ShowMessage("Precio inválido", "#ff0033");
                    ShowLoader(false);
                    return;
                }

                precioManual = precio;
            }


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



            //Precio Opcional
            if (precioManual.HasValue)
            {
                //content.Add(new StringContent(precioManual.Value.ToString()), "precioManual");
                content.Add(
                    new StringContent(
                        precioManual.Value.ToString(CultureInfo.InvariantCulture)), "precioManual");
            }



            var fileContent = new StreamContent(memoryStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            content.Add(fileContent, "image", "qr.jpg"); // "image" debe coincidir con el parámetro IFormFile

            // Enviar a la API y sonido de camara
            var response = await _httpClient.PostAsync("https://192.168.2.211:7203/api/lector/imagen", content);

            //await PlaySound(SoundType.CameraShot);
           //var response = await _httpClient.PostAsync("https://localhost:7203/api/lector/imagen", content);

            var mensaje = await ObtenerMensajeDeRespuesta(response);

            if (response.IsSuccessStatusCode)
            {
                await PlaySound(SoundType.Success);
                chkPrecioManual.IsChecked = false;
                txtPrecioManual.Text = string.Empty;
                txtPrecioManual.IsEnabled = false;
                await ShowMessage("✔ " + mensaje, "#1e1e2f");
            }
            else
            {
                await PlaySound(SoundType.Error);
                await ShowMessage("⚠ " + mensaje, "#ff0033");
            }
        }
        catch (Exception ex)
        {
            await PlaySound(SoundType.Error);
            await ShowMessage("Error: " + ex.Message, "#ff0033");
        }
        finally
        {
            ShowLoader(false);
        }
    }
    #endregion

    #region MuestraCarga
    private void ShowLoader(bool visible)
    {
        loader.IsVisible = visible;
        loader.IsRunning = visible;
        overlay.IsVisible = visible;
    }
    #endregion

    #region MostrarMensajesRespuestasJson
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
    #endregion

    #region RespuestasJson
    /*Manejo de respuestas*/
    private async Task<string> ObtenerMensajeDeRespuesta(HttpResponseMessage response)
    {
        if (response == null)
            return "Oops, algo salió mal. Intenta nuevamente.";

        string contenido;

        try
        {
            contenido = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return "Oops, no se pudo leer la respuesta del servidor.";
        }

        if (string.IsNullOrWhiteSpace(contenido))
            return "Oops, el servidor no devolvió información.";

        try
        {
            using var doc = JsonDocument.Parse(contenido);
            var root = doc.RootElement;

            // Soporta { mensaje: "..." }
            if (root.TryGetProperty("mensaje", out var mensaje))
                return mensaje.GetString() ?? MensajeGenerico();

            // Soporta { message: "..." }
            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? MensajeGenerico();

            // Soporta { error: "..." }
            if (root.TryGetProperty("error", out var error))
                return error.GetString() ?? MensajeGenerico();

            // Si es JSON pero no tiene nada útil
            return MensajeGenerico();
        }
        catch
        {
            // No es JSON (HTML, texto plano, etc.)
            return MensajeGenerico();
        }
    }

    private string MensajeGenerico()
    {
        return "Oops, algo salió mal. Intenta nuevamente.";
    }
    #endregion

    #region MostrarHistorial
    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        try
        {
            var hoy = DateTime.Today.ToString("yyyy-MM-dd");
            var url = $"https://192.168.2.211:7203/api/lector/historial?fecha={hoy}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await _soundService.PlayResultSound(SoundType.Error);
                await ShowMessage("No se pudo obtener el historial", "#ff0033");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            await _soundService.PlayResultSound(SoundType.Navigation);
            await Navigation.PushAsync(new HistorialPage(json, _soundService));
        }
        catch (Exception ex)
        {
            await _soundService.PlayResultSound(SoundType.Error);
            await ShowMessage(ex.Message, "#ff0033");
        }
    }

  
    #endregion

    #region AgregarPrecioManual
    private void OnPrecioManualChecked(object sender, CheckedChangedEventArgs e)
    {
        txtPrecioManual.IsEnabled = e.Value;
        txtPrecioManual.BackgroundColor = e.Value
            ? Colors.White
            : Color.FromArgb("#f4f6f9");

        if (!e.Value)
            txtPrecioManual.Text = string.Empty;
    }
    #endregion

    #region Sonidos


    #region Precarga
   


    private async Task PreloadCameraSound()
    {
        try
        {
            if (_preloadedShotPlayer != null)
                return;

            var stream = await FileSystem.OpenAppPackageFileAsync("shot.mp3");
            _preloadedShotPlayer = _audioManager.CreatePlayer(stream);
            _preloadedShotPlayer.Volume = 1.0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Preload audio error: {ex.Message}");
        }
    }


    private Task PlaySound(SoundType soundType)
    {
        try
        {
            if (soundType == SoundType.CameraShot)
            {
                if (_preloadedShotPlayer == null)
                    return Task.CompletedTask;

                _preloadedShotPlayer.Stop(); // por si acaso
                _preloadedShotPlayer.Play();
                return Task.CompletedTask;
            }

            // Success / Error siguen igual
            return PlayResultSound(soundType);
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    #endregion



    public async Task PlayResultSound(SoundType soundType)
    {
        

        string fileName = soundType switch
        {
            SoundType.Success=> "success.mp3",
            SoundType.Error=> "error.mp3",
            SoundType.Navigation=> "navigation.mp3",
            _=>"navigation.mp3"

        };

        var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

        lock (_resultLock)
        {
            _resultPlayer?.Stop();
            _resultPlayer?.Dispose();
            _resultPlayer = _audioManager.CreatePlayer(stream);
            _resultPlayer.Volume = 1.0;
            _resultPlayer.Play();
        }
    }

   
    #endregion


}


