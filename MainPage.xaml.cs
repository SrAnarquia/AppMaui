using AppScanner.Services;

namespace AppScanner;

public enum TipoOperacion
{
    Comida,
    Desayuno,
    Pedido
}

public partial class MainPage : ContentPage
{
    private readonly SoundService _soundService;

    public MainPage(SoundService soundService)
    {
        InitializeComponent();
        _soundService = soundService;
    }

    private async void OnOperacionTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame)
            return;

        // Animación elegante
        await frame.ScaleTo(0.96, 80, Easing.CubicIn);
        await frame.ScaleTo(1, 120, Easing.CubicOut);

        // Sonido navegación
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