using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class ImportPage : LUC
{
    private string file = string.Empty;

    public ImportPage()
    {
        InitializeComponent();
        Loaded += (_, _) => ImportFile.Text = file;
    }

    public ImportPage WithFile(string fileName)
    {
        file = fileName;
        return this;
    }

    private async void BrowseInputFolder(object? sender, RoutedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Main?.GetStorageProvider() is not { } storage) return;
            if (!storage.CanPickFolder) return;

            var folder = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Lang.Lang.Start_SelectFolderToSort,
                SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(InputFolder.Text!),
                AllowMultiple = false
            });

            if (folder.Count < 0) return;

            await Dispatcher.UIThread.InvokeAsync(() => InputFolder.Text = folder[0].TryGetLocalPath());
        });
    }

    private async void BrowseOutputFolder(object? sender, RoutedEventArgs e)
    {
        if (Main?.GetStorageProvider() is not { CanPickFolder: true } storage) return;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var folder = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Lang.Lang.Start_SelectOutputFolder,
                SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(OutputFolder.Text!),
                AllowMultiple = false
            });

            if (folder.Count < 0) return;

            await Dispatcher.UIThread.InvokeAsync(() => OutputFolder.Text = folder[0].TryGetLocalPath());
        });
    }

    private async void ImportFileBrowse(object? sender, RoutedEventArgs e)
    {
        if (Main?.GetStorageProvider() is not { CanPickFolder: true } storage) return;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var folder = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Lang.Lang.Import_DialogTitle,
                SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(ImportFile.Text!),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Lang.Lang.Import_FileTypeName)
                    {
                        Patterns = new[] { "*.blp" },
                        MimeTypes = new[] { "application/blp" },
                        AppleUniformTypeIdentifiers = new[] { "com.haltroy.bluelabel.project" }
                    },
                    FilePickerFileTypes.All
                }
            });

            if (folder.Count < 0) return;

            await Dispatcher.UIThread.InvokeAsync(() => ImportFile.Text = folder[0].TryGetLocalPath());
        });
    }


    private void Next(object? sender, RoutedEventArgs e)
    {
        if (Main is null || UseInputFolder is null || ImportFile is null || InputFolder is null ||
            OutputFolder is null || string.IsNullOrWhiteSpace(ImportFile.Text) ||
            string.IsNullOrWhiteSpace(InputFolder.Text) ||
            string.IsNullOrWhiteSpace(OutputFolder.Text)) return;
        try
        {
            var settings = new LabelerSetting
            {
                InputFolder = InputFolder.Text,
                OutputFolder = OutputFolder.Text,
                SortInInputFolder = UseInputFolder.IsChecked is true
            }.Load(ImportFile.Text);
            Main.ShowControl(new LoadingScreen().WithAction(Tools.Automation(settings, Main, this)).ReturnBackTo(this));
        }
        catch (Exception ex)
        {
            Main.ShowControl(new ErrorScreen().WithError(ex.ToString())
                .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main.ShowControl(this))).ReturnBackTo(this));
        }
    }

    private void Back(object? s, RoutedEventArgs e)
    {
        Main?.ShowControl(ReturnTo ?? new StartPage());
    }

    private void CheckIfCorrect()
    {
        if (NextButton is null) return;
        NextButton.IsEnabled = InputFolder is not null && OutputFolder is not null && ImportFile is not null &&
                               UseInputFolder is not null &&
                               !string.IsNullOrWhiteSpace(InputFolder.Text) &&
                               !string.IsNullOrWhiteSpace(ImportFile.Text) &&
                               File.Exists(ImportFile.Text) &&
                               Directory.Exists(InputFolder.Text) &&
                               (UseInputFolder.IsChecked is not false ||
                                (!string.IsNullOrWhiteSpace(OutputFolder.Text) && Directory.Exists(OutputFolder.Text)));
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        CheckIfCorrect();
    }

    private void UseInputFolder_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        CheckIfCorrect();
    }
}