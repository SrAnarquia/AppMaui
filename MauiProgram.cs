using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AppScanner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                 .UseMauiCommunityToolkit()
                 .UseMauiCommunityToolkitCamera()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // HttpClient que ignora validación de certificados en TODOS los modos
            var handler = new HttpClientHandler 
            { 
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true 
            };

            builder.Services.AddSingleton(new HttpClient(handler));


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
