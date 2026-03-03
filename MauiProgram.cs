using AppScanner.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

using Plugin.Maui.Audio;

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

            /*Servicios*/
            builder.Services.AddSingleton(new HttpClient(handler));
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<SoundService>();
            builder.Services.AddSingleton<ScannerService>();


            /*Registro de paginas*/
            builder.Services.AddTransient<ComidaPage>();
            builder.Services.AddTransient<DesayunoPage>();
            builder.Services.AddTransient<PedidoPage>();
            builder.Services.AddTransient<MainPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
