using System.Windows;
using MainViewModel = ToApp.ViewModels.MainViewModel;

namespace ToApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var vm = new MainViewModel();

        var window = new MainWindow
        {
            DataContext = vm
        };

        window.Show();
        vm.LoadCommand.Execute(null);
    }
}
