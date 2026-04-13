using System.Windows;

namespace ToApp.Services;

public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string message, string title = "Информация")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message, string title = "Ошибка")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string message, string title = "Подтверждение")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<string?> PromptAsync(string message, string title = "Ввод")
    {
        return Task.FromResult(Microsoft.VisualBasic.Interaction.InputBox(message, title));
    }
}
