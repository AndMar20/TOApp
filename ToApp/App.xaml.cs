using System.Windows;
using ToApp.Services;
using ToApp.ViewModels;

namespace ToApp;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dialogService = new DialogService();
        var inventoryService = new InventoryService();
        var vm = new MainViewModel(inventoryService, dialogService);

        var window = new MainWindow
        {
            DataContext = vm
        };

        window.Show();
        await vm.LoadAllAsync();
    }
}
