using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class NewLabelProject : LUC
{
    private readonly LabelerSetting CurrentSettings = new();

    public NewLabelProject()
    {
        InitializeComponent();
        NamingFormat.ItemsSource = new[]
        {
            "%label%",
            "%id%",
            "%label% - %name%",
            "%label%.%name%",
            "%label% (%name%)",
            "%label% - %id%",
            "%label%.%id%",
            "%label% (%id%)"
        };
    }

    private void Back(object? s, RoutedEventArgs e)
    {
        Main?.ShowControl(ReturnTo ?? new StartPage());
    }

    private void AutomateSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
            CurrentSettings.Automation = (AutomationType)cb.SelectedIndex;
    }

    private void OperationModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
            CurrentSettings.Operation = (OperationType)cb.SelectedIndex;
    }

    private void AutomationDurationOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var format = "d\\:hh\\:mm\\:ss\\.fffffff";
        if (sender is TextBox { Text: { } s } tb)
        {
            if (TimeSpan.TryParseExact(s, format, CultureInfo.CurrentCulture, out var duration))
            {
                IncorrectDuration.IsVisible = false;
                CurrentSettings.AutomationDuration = duration;
                tb.Text = duration.ToString(format);
            }
            else
            {
                IncorrectDuration.IsVisible = true;
            }
        }
    }

    private void AutomationWidthOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown { Value: not null } nud)
            CurrentSettings.AutomateImageSizeMinWidth = (int)nud.Value;
    }

    private void AutomationHeightOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown { Value: not null } nud)
            CurrentSettings.AutomateImageSizeMinHeight = (int)nud.Value;
    }

    private void AutomationFileSizeOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown { Value: not null } nud &&
            ByteType?.SelectedItem is ComboBoxItem { Tag: string s } &&
            long.TryParse(s, NumberStyles.None, null, out var power))
            CurrentSettings.AutomateFileSizeMinSize = (long)nud.Value * power;
    }

    private void ByteType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AutomationFileSize?.Value != null && sender is ComboBox { SelectedItem: ComboBoxItem { Tag: string s } } &&
            long.TryParse(s, NumberStyles.None, null, out var power))
            CurrentSettings.AutomateFileSizeMinSize = (long)AutomationFileSize.Value * power;
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

    private async void Export(object? s, RoutedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Main?.GetStorageProvider() is not { } storage) return;
            if (!storage.CanPickFolder) return;

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Lang.Lang.Start_ExportTitle,
                FileTypeChoices = new[]
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

            if (file is null || file.TryGetLocalPath() is not { } s) return;

            CurrentSettings.Save(s);
        });
    }

    private void UseInputFolderOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.SortInInputFolder = ts.IsChecked is true;
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

    private void InputFolderOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
            CurrentSettings.InputFolder = tb.Text;
    }

    private void OutputFolderOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
            CurrentSettings.OutputFolder = tb.Text!;
    }

    private void AllowSearchingSubfolders_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.AllowSearchingSubfolders = ts.IsChecked is true;
    }

    private void SearchIncludeFilters_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.UseFilters = ts.IsChecked is true;
    }

    private void Filters_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
            CurrentSettings.Filter = tb.Text.Split(',');
    }

    private void ProjectActionSubfolder_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
            CurrentSettings.LabelFilesBy = rb.IsChecked is true
                ? LabelFilesOptions.Subfolder
                : LabelFilesOptions.Rename;
    }

    private void EnableSubfolderInSubfolder_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.AllowRecursiveSubfolders = ts.IsChecked is true;
    }

    private void ProjectActionRename_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
            CurrentSettings.LabelFilesBy = rb.IsChecked is true
                ? LabelFilesOptions.Rename
                : LabelFilesOptions.Subfolder;
    }

    private void NamingFormat_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is AutoCompleteBox acb && !string.IsNullOrWhiteSpace(acb.Text))
            CurrentSettings.RenameFilesWith = acb.Text!;
    }

    private void Next(object? sender, RoutedEventArgs e)
    {
        if (Main is null) return;
        Main.ShowControl(new LoadingScreen().WithAction(Tools.Automation(CurrentSettings, Main, this))
            .ReturnBackTo(this));
    }

    private void AutomateFileTypeUseExt_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.AutomateFileTypeUseExtensions = ts.IsChecked is true;
    }
}