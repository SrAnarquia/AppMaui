using AppScanner.Services;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AppScanner;

public partial class PedidoPage : ContentPage
{
    #region Builder
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private readonly SoundService _soundService;
    private readonly ScannerService _scannerService;

    private IAudioPlayer? _resultPlayer;
    private IAudioPlayer? _preloadedShotPlayer;
    private readonly object _resultLock = new();

  
    public PedidoPage(
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

    #region PermisosCamara
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.Camera>();

        await PreloadCameraSound();
    }
    #endregion

    #region CapturarYEnviar
    private async void OnCaptureAndSend(object sender, EventArgs e)
    {
        try
        {
            ShowLoader(true);
            await PlaySound(SoundType.CameraShot);

            // ✅ Validar servicio
            if (pickerServicio.SelectedItem == null)
            {
                await ShowMessage("Selecciona tipo de servicio", "#ff0033");
                return;
            }

            // ✅ Validar precio
            if (!decimal.TryParse(txtPrecio.Text, out var precio) || precio <= 0)
            {
                await ShowMessage("Precio inválido", "#ff0033");
                return;
            }

            // ✅ Validar producto
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                await ShowMessage("Especifica el producto", "#ff0033");
                return;
            }

            // ✅ Validar cantidad
            if (!int.TryParse(txtCantidad.Text, out var cantidad) || cantidad <= 0)
            {
                await ShowMessage("Cantidad inválida", "#ff0033");
                return;
            }

            string servicio = pickerServicio.SelectedItem.ToString();
            string descripcion = txtDescripcion.Text.Trim();
            int cantidadStr = cantidad;
            decimal precioManual = precio;

            var stream = await cameraView.CaptureImage(CancellationToken.None);
            if (stream == null)
            {
                await ShowMessage("No se pudo capturar imagen", "#ff0033");
                return;
            }

            var (success, message) = await _scannerService.EnviarImagenAsync(
                stream,
                precioManual: precioManual,
                servicio: servicio,
                descripcion: descripcion,
                cantidad: cantidadStr
            );

            if (success)
            {
                // ✅ Reset campos
                pickerServicio.SelectedIndex = -1;
                txtPrecio.Text = "";
                txtDescripcion.Text = "";
                txtCantidad.Text = "1";

                await PlaySound(SoundType.Success);
                await ShowMessage("✔ " + message, "#1e1e2f");
            }
            else
            {
                await PlaySound(SoundType.Error);
                await ShowMessage("✖ " + message, "#ff0033");
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

    #region Loader
    private void ShowLoader(bool visible)
    {
        loader.IsVisible = visible;
        loader.IsRunning = visible;
        overlay.IsVisible = visible;
    }
    #endregion

    #region Mensajes
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

    #region Historial
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

    #region Sonidos
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
                _preloadedShotPlayer?.Stop();
                _preloadedShotPlayer?.Play();
                return Task.CompletedTask;
            }

            return PlayResultSound(soundType);
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

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
        await _soundService.PlayResultSound(SoundType.Navigation);
        await Navigation.PopAsync();
    }
    #endregion

    #region CantidadAumento
    private void OnMasClicked(object sender, EventArgs e)
    {
        int cantidad = 1;

        if (!string.IsNullOrEmpty(txtCantidad.Text))
            int.TryParse(txtCantidad.Text, out cantidad);

        cantidad++;
        txtCantidad.Text = cantidad.ToString();
    }

    private void OnMenosClicked(object sender, EventArgs e)
    {
        int cantidad = 1;

        if (!string.IsNullOrEmpty(txtCantidad.Text))
            int.TryParse(txtCantidad.Text, out cantidad);

        if (cantidad > 1)
            cantidad--;

        txtCantidad.Text = cantidad.ToString();
    }
    #endregion

}