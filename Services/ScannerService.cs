using System.Globalization;
using System.Text.Json;

namespace AppScanner.Services;

public class ScannerService
{
    private readonly HttpClient _httpClient;

    public ScannerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool success, string message)> EnviarImagenAsync(
        Stream imageStream,
        decimal? precioManual,
        string? servicio,
        string? descripcion)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var content = new MultipartFormDataContent();

            //Se agrega contenido de precio como parametro a la API
            if (precioManual.HasValue)
            {
                content.Add(
                    new StringContent(
                        precioManual.Value.ToString(CultureInfo.InvariantCulture)),
                    "precioManual");
            }

            var fileContent = new StreamContent(memoryStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            //Se agrega contenido de imagen a la API
            content.Add(fileContent, "image", "qr.jpg");

           
            //Se agrega contenido de servicio
            content.Add(new StringContent(servicio), "servicio");

            //Se agrega descripcion de servicio
            content.Add(new StringContent(descripcion), "descripcion");



            //Se agrega contenido
            var response = await _httpClient.PostAsync(
                "https://192.168.2.211:7203/api/lector/imagen",
                content);

            var mensaje = await ObtenerMensaje(response);

            return (response.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<string> ObtenerMensaje(HttpResponseMessage response)
    {
        if (response == null)
            return "Oops, algo salió mal.";

        var contenido = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(contenido))
            return "Servidor sin respuesta.";

        try
        {
            using var doc = JsonDocument.Parse(contenido);
            var root = doc.RootElement;

            if (root.TryGetProperty("mensaje", out var mensaje))
                return mensaje.GetString() ?? "Sin mensaje.";

            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? "Sin mensaje.";

            if (root.TryGetProperty("error", out var error))
                return error.GetString() ?? "Sin mensaje.";

            return "Operación finalizada.";
        }
        catch
        {
            return "Respuesta inválida del servidor.";
        }
    }
}