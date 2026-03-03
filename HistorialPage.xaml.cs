using AppScanner.Services;
using System.Text.Json;

namespace AppScanner;

public partial class HistorialPage : ContentPage
{

    private readonly SoundService _soundService;

    public HistorialPage(string json,SoundService soundService)
    {
        InitializeComponent();
        _soundService = soundService;
        var doc = JsonDocument.Parse(json);
        var datos = doc.RootElement.GetProperty("datos");

        var lista = new List<HistorialItem>();

        foreach (var item in datos.EnumerateArray())
        {
            lista.Add(new HistorialItem
            {
                Nombre = item.GetProperty("nombre").GetString(),
                Precio = item.GetProperty("precio").GetDecimal(),
                Folio = item.GetProperty("folio").GetInt32(),
                Hora = 
                item.GetProperty("hora").GetString()
            });
        }

        HistorialList.ItemsSource = lista;
    }

    private async void OnBack(object sender, EventArgs e)
    {
        // Primero reproducimos el sonido de navegación
        await _soundService.PlayResultSound(SoundType.Navigation);
        await Navigation.PopAsync();
    }

    private async void OnPlaySoundClicked(object sender, EventArgs e) 
    { 
        await _soundService.PlayResultSound(SoundType.Success); 
    }

}

public class HistorialItem
{
    public string Nombre { get; set; }

    public decimal Precio { get; set; }

    public int Folio { get; set; }
    public string Hora { get; set; }
}