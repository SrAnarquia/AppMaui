using AppScanner.Services;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AppScanner;

public partial class DesayunoPage : ContentPage
{

    #region Builder
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private readonly SoundService _soundService;

    private readonly ScannerService _scannerService;

    private IAudioPlayer? _resultPlayer;

    private readonly object _resultLock = new();

    private IAudioPlayer? _preloadedShotPlayer;

    public DesayunoPage(
    HttpClient httpClient,
    ScannerService scannerService,
    IAudioManager audioManager,
    SoundService soundService)
    {
        InitializeComponent();
        _scannerService = scannerService;
        _audioManager = audioManager;
        _soundService = soundService;
        _httpClient = httpClient;
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

            //Parametros para registrar
            decimal? precioManual = null;
            string? descripcion = "";
            int? cantidad = 1;
            string? servicio = "Desayuno";

            if (swPrecioManual.IsToggled)
            {
                if (!decimal.TryParse(txtPrecioManual.Text, out var precio) || precio <= 0)
                {
                    await ShowMessage("Precio inválido", "#ff0033");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
                {
                    await ShowMessage("Agrega una descripción breve", "#ff0033");
                    return;
                }

                precioManual = precio;
                descripcion = txtDescripcion.Text.Trim();
            }

            var stream = await cameraView.CaptureImage(CancellationToken.None);

            if (stream == null)
            {
                await ShowMessage("No se pudo capturar imagen", "#ff0033");
                return;
            }

            var (success, message) = await _scannerService.EnviarImagenAsync(stream, precioManual, servicio, descripcion,cantidad);

            if (success)
            {
                //  Si se usó precio manual, limpiar campos para evitar doble envío
                if (swPrecioManual.IsToggled)
                {
                    txtPrecioManual.Text = string.Empty;
                    txtDescripcion.Text = string.Empty;

                    // Opcional: apagar el switch automáticamente
                    swPrecioManual.IsToggled = false;
                }



                await PlaySound(SoundType.Success);
                await ShowMessage("? " + message, "#1e1e2f");
            }
            else
            {
                await PlaySound(SoundType.Error);
                await ShowMessage("? " + message, "#ff0033");
            }
        }
        catch (Exception ex)
        {
            await PlaySound(SoundType.Error);
            await ShowMessage(ex.Message, "#ff0033");
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
    private async void OnPrecioManualToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            precioManualContainer.IsVisible = true;
            await precioManualContainer.FadeTo(1, 200);
        }
        else
        {
            await precioManualContainer.FadeTo(0, 200);
            precioManualContainer.IsVisible = false;

            txtPrecioManual.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
        }
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
            SoundType.Success => "success.mp3",
            SoundType.Error => "error.mp3",
            SoundType.Navigation => "navigation.mp3",
            _ => "navigation.mp3"

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

    #region Retroceder
    private async void OnBack(object sender, EventArgs e)
    {
        // Primero reproducimos el sonido de navegación
        await _soundService.PlayResultSound(SoundType.Navigation);
        await Navigation.PopAsync();
    }


    #endregion

}


