using System.Windows;
using ToApp.ViewModels;

namespace ToApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
