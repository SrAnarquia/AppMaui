using System.Globalization;

namespace AppScanner.Converters;

public class ServicioIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Comida" => "comida.png",
            "Desayuno" => "desayuno.png",
            "Pedido" => "pedido.png",
            _ => "pedido.png"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}