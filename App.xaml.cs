using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MarkView.Helpers;
using MarkView.Views;
using MarkView.ViewModels;

namespace MarkView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            services.AddMarkViewServices();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow();
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;

            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
