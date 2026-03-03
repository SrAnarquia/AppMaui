using Microsoft.Extensions.DependencyInjection;

namespace AppScanner
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(ComidaPage), typeof(ComidaPage));
            Routing.RegisterRoute(nameof(DesayunoPage), typeof(DesayunoPage));
            Routing.RegisterRoute(nameof(PedidoPage), typeof(PedidoPage));

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }



    }
}
