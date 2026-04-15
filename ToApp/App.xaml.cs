using System.Windows;
using ToApp.Services;
using ToApp.ViewModels;

namespace ToApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var vm = new MainViewModel(new InventoryService(), new DialogService());

        var window = new MainWindow
        {
            DataContext = vm
        };

        window.Show();
    }
}
