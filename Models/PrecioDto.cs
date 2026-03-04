using System.Text.Json.Serialization;

namespace AppScanner.Models;

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