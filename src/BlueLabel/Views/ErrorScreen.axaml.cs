using System;
using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class ErrorScreen : LUC
{
    private Action? ContinueAction;
    private string errorText = string.Empty;

    public ErrorScreen()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ErrorMessage.Text = errorText;
            if (ContinueAction is null) ContinueButton.IsEnabled = false;
        };
    }

    public ErrorScreen WithError(string error)
    {
        errorText = error;
        return this;
    }

    public ErrorScreen WithContinue(Action continueAction)
    {
        ContinueAction = continueAction;
        return this;
    }

    private async void SavetoFile(object? sender, RoutedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Main?.GetStorageProvider() is not { CanPickFolder: true } storage) return;

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Lang.Lang.Error_SaveOutputTo,
                FileTypeChoices = new[] { FilePickerFileTypes.TextPlain },
                ShowOverwritePrompt = true
            });

            if (file is null) return;

            if (!File.Exists(file.Path.AbsolutePath)) File.Create(file.Path.AbsolutePath).Close();
            await using var fs = new FileStream(file.Path.AbsolutePath, FileMode.Truncate, FileAccess.Write,
                FileShare.ReadWrite);
            await using var writer = new StreamWriter(fs);
            await writer.WriteAsync(errorText);
        });
    }

    private void Continue(object? sender, RoutedEventArgs e)
    {
        ContinueAction?.Invoke();
    }
}