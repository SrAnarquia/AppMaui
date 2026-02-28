using System.Text.Json;

namespace AppScanner;

public partial class HistorialPage : ContentPage
{
    public HistorialPage(string json)
    {
        InitializeComponent();

        var doc = JsonDocument.Parse(json);
        var datos = doc.RootElement.GetProperty("datos");

        var lista = new List<HistorialItem>();

        foreach (var item in datos.EnumerateArray())
        {
            lista.Add(new HistorialItem
            {
                Nombre = item.GetProperty("nombre").GetString(),
                Precio=item.GetProperty("precio").GetDecimal(),
                Hora = item.GetProperty("hora").GetString()
            });
        }

        HistorialList.ItemsSource = lista;
    }

    private async void OnBack(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class HistorialItem
{
    public string Nombre { get; set; }

    public decimal Precio { get; set; }
    public string Hora { get; set; }
}