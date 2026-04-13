namespace ToApp.Services;

public interface IDialogService
{
    Task ShowInfoAsync(string message, string title = "Информация");
    Task ShowErrorAsync(string message, string title = "Ошибка");
    Task<bool> ConfirmAsync(string message, string title = "Подтверждение");
    Task<string?> PromptAsync(string message, string title = "Ввод");
}
