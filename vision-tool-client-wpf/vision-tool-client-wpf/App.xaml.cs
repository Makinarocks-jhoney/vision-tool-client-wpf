using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;


namespace vision_tool_client_wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var services = new ServiceCollection()
    .AddSingleton<IWindowService, WindowService>()
    .AddTransient<StartUpViewModel>()
    .AddTransient<MainWindowViewModel>()
    .BuildServiceProvider();

            Ioc.Default.ConfigureServices(services);


            var startUp = new StartUp
            {
                DataContext = Ioc.Default.GetRequiredService<StartUpViewModel>()
            };
            startUp.Show();
            //var mainWindow = new MainWindow
            //{
            //    DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>()
            //};
            //mainWindow.Show();
        }
    }

}
