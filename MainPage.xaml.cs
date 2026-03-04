using AppScanner.Services;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AppScanner;

public enum TipoOperacion
{
    Comida,
    Desayuno,
    Pedido
}

public class PrecioDto
{
    [JsonPropertyName("servicio")]
    public string Servicio { get; set; }

    [JsonPropertyName("precioActual")]
    public decimal PrecioActual { get; set; }
}

public class PreciosResponse
{
    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; }

    [JsonPropertyName("precios")]
    public List<PrecioDto> Precios { get; set; }
}

public partial class MainPage : ContentPage
{
    private readonly SoundService _soundService;

    private readonly HttpClient _http = new HttpClient(
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true
        });

    public MainPage(SoundService soundService)
    {
        InitializeComponent();
        _soundService = soundService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPrecios();
    }

    private async Task CargarPrecios()
    {
        try
        {
            var url = "https://192.168.2.211:7203/api/lector/precios";

            var response = await _http.GetFromJsonAsync<PreciosResponse>(url);

            if (response == null)
            {
                LblPrecioComida.Text = "0.00";
                LblPrecioDesayuno.Text = "0.00";
                return;
            }

            var comida = response.Precios.FirstOrDefault(x => x.Servicio == "Comida");
            var desayuno = response.Precios.FirstOrDefault(x => x.Servicio == "Desayuno");

            LblPrecioComida.Text = comida != null ? comida.PrecioActual.ToString("0.00") : "0.00";
            LblPrecioDesayuno.Text = desayuno != null ? desayuno.PrecioActual.ToString("0.00") : "0.00";
        }
        catch
        {
            LblPrecioComida.Text = "0.00";
            LblPrecioDesayuno.Text = "0.00";
        }
    }

    private async void OnOperacionTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame)
            return;

        await frame.ScaleTo(0.96, 80, Easing.CubicIn);
        await frame.ScaleTo(1, 120, Easing.CubicOut);

        await _soundService.PlayResultSound(SoundType.Navigation);

        if (!Enum.TryParse<TipoOperacion>(e.Parameter?.ToString(), out var tipo))
            return;

        await Redireccionar(tipo);
    }

    private async Task Redireccionar(TipoOperacion tipo)
    {
        switch (tipo)
        {
            case TipoOperacion.Comida:
                await Shell.Current.GoToAsync(nameof(ComidaPage));
                break;

            case TipoOperacion.Desayuno:
                await Shell.Current.GoToAsync(nameof(DesayunoPage));
                break;

            case TipoOperacion.Pedido:
                await Shell.Current.GoToAsync(nameof(PedidoPage));
                break;
        }
    }
}