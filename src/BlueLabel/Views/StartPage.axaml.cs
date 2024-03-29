using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using DynamicData;
using LibVLCSharp.Shared;

namespace BlueLabel.Views;

public partial class StartPage : LUC
{
    private readonly LabelerSetting CurrentSettings = new();

    public StartPage()
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
        if (Application.Current is not { } app) return;
        if (app.RequestedThemeVariant == ThemeVariant.Default)
            ThemeSystemDefault.IsChecked = true;
        else if (app.RequestedThemeVariant == ThemeVariant.Light)
            ThemeLight.IsChecked = true;
        else if (app.RequestedThemeVariant == ThemeVariant.Dark)
            ThemeDark.IsChecked = true;
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
        // Yeah this is scary but we do handle each errors here so there shouldn't be a problem. 
        // Also see comments below.
        // ReSharper disable once AsyncVoidLambda
        Main.ShowControl(new LoadingScreen().WithAction(async status =>
        {
            status.Title = Lang.Lang.Status_GettingFiles;
            status.IsIndeterminate = true;
            status.WorkingOn = Lang.Lang.Status_GettingFiles;

            List<string> errors = new();
            var files = Array.Empty<LabelFile>();

            try
            {
                files = CurrentSettings.GetFiles();
            }
            catch (Exception ex)
            {
                errors.Add(Lang.Lang.Error_GettingFiles.Replace("$ex$", ex.ToString()));
            }

            if (errors.Count > 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Main?.ShowControl(new ErrorScreen().WithError(string.Join(Environment.NewLine, errors.ToArray()))
                        .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main?.ShowControl(this)))
                        .ReturnBackTo(this));
                });
                return;
            }

            // We grouped them into one single section below because they all use same labels.
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (CurrentSettings.Automation)
            {
                case AutomationType.Manual:
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Main?.ShowControl(new PreviewMain().WithListAndSettings(CurrentSettings, files)
                            .ReturnBackTo(this));
                    });
                    break;
                case AutomationType.AutomateByFileType:
                {
                    status.Title = Lang.Lang.AutoSort_Title;
                    status.IsIndeterminate = false;
                    if (CurrentSettings.AutomateFileTypeUseExtensions)
                    {
                        List<Label> labels = new();

                        for (var i = 0; i < files.Length; i++)
                        {
                            try
                            {
                                status.WorkingOn = files[i].OriginalFileName;

                                if (labels.FindAll(it => string.Equals(it.Name, files[i].FileExtension.Substring(1))) is
                                    { Count: > 0 } list)
                                {
                                    files[i].Labels.Add(list.ToArray());
                                }
                                else
                                {
                                    var label = new Label(files[i].FileExtension.Substring(1));
                                    labels.Add(label);
                                    files[i].Labels.Add(label);
                                }

                                files[i].TargetFile(CurrentSettings);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(
                                    Lang.Lang.AutoSort_ErrorOnFile.Replace("$file_path$", files[i].OriginalPath)
                                        .Replace("$ex$", ex.ToString()));
                            }

                            status.Percentage = i * 100 / files.Length;

                            Thread.Sleep(10);
                        }

                        if (files.Length <= 0)
                            errors.Add(Lang.Lang.Error_NoFilesLoaded);

                        if (errors.Count > 0)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                Main?.ShowControl(new ErrorScreen()
                                    .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                    .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main?.ShowControl(this)))
                                    .ReturnBackTo(this));
                            });
                            return;
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main?.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                                .ReturnBackTo(this));
                        });
                    }
                    else
                    {
                        var videoLabel = new Label(Lang.Lang.AutoSort_Video);
                        var audioLabel = new Label(Lang.Lang.AutoSort_Audio);
                        var textLabel = new Label(Lang.Lang.AutoSort_Text);
                        var archiveLabel = new Label(Lang.Lang.AutoSort_Archive);
                        var imageLabel = new Label(Lang.Lang.AutoSort_Image);
                        var unknownLabel = new Label(Lang.Lang.AutoSort_Unknown);

                        for (var i = 0; i < files.Length; i++)
                        {
                            try
                            {
                                status.WorkingOn = files[i].OriginalFileName;
                                switch (files[i].Type)
                                {
                                    default:
                                    case FileType.Unsupported:
                                        files[i].Labels.Add(unknownLabel);
                                        break;
                                    case FileType.Archive:
                                        files[i].Labels.Add(archiveLabel);
                                        break;
                                    case FileType.Audio:
                                        files[i].Labels.Add(audioLabel);
                                        break;
                                    case FileType.Image:
                                        files[i].Labels.Add(imageLabel);
                                        break;
                                    case FileType.Video:
                                        files[i].Labels.Add(videoLabel);
                                        break;
                                    case FileType.Text:
                                        files[i].Labels.Add(textLabel);
                                        break;
                                }

                                files[i].TargetFile(CurrentSettings);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(Lang.Lang.AutoSort_ErrorOnFile.Replace("$file_path$", files[i].OriginalPath)
                                    .Replace("$ex$", ex.ToString()));
                            }

                            status.Percentage = i * 100 / files.Length;

                            Thread.Sleep(10);
                        }

                        if (files.Length <= 0)
                            errors.Add(Lang.Lang.Error_NoFilesLoaded);

                        if (errors.Count > 0)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                Main?.ShowControl(new ErrorScreen()
                                    .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                    .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main?.ShowControl(this)))
                                    .ReturnBackTo(this));
                            });
                            return;
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main?.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                                .ReturnBackTo(this));
                        });
                    }

                    break;
                }
                default:
                {
                    status.Title = Lang.Lang.AutoSort_Title;
                    status.IsIndeterminate = false;
                    var smaller = new Label(Lang.Lang.AutoSort_Smaller);
                    var bigger = new Label(Lang.Lang.AutoSort_Bigger);
                    var unknown = new Label(Lang.Lang.AutoSort_Unknown);
                    for (var i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            var file = files[i];
                            status.WorkingOn = file.OriginalFileName;

                            // We did on the previous entries.
                            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                            switch (CurrentSettings.Automation)
                            {
                                case AutomationType.AutomateByDuration:
                                    if (file.Type is FileType.Audio or FileType.Video)
                                        // If we don'T make this await, it will just skip the entire labeling and
                                        // try get the file name which will fail.
                                        // If we don't put this code in ui thread, getting main's LibVLC
                                        // will throw invalid operation exception.
                                        await Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            var media = new Media(Main.LibVlc, file.OriginalPath);
                                            media.Parse();
                                            file.Labels.Add(
                                                media.Duration >= CurrentSettings.AutomationDuration.TotalMilliseconds
                                                    ? bigger
                                                    : smaller);
                                        });
                                    else
                                        file.Labels.Add(unknown);

                                    break;
                                case AutomationType.AutomateByFileSize:
                                    file.Labels.Add(new FileInfo(file.OriginalPath).Length >=
                                                    CurrentSettings.AutomateFileSizeMinSize
                                        ? bigger
                                        : smaller);
                                    break;
                                case AutomationType.AutomateByImageSize:
                                    switch (file.Type)
                                    {
                                        case FileType.Image:
                                        {
                                            var img = new Bitmap(file.OriginalPath);
                                            file.Labels.Add(
                                                img.Size.Height >= CurrentSettings.AutomateImageSizeMinHeight &&
                                                img.Size.Width >= CurrentSettings.AutomateImageSizeMinWidth
                                                    ? bigger
                                                    : smaller);
                                            break;
                                        }
                                        case FileType.Video:
                                        {
                                            // Same as above
                                            await Dispatcher.UIThread.InvokeAsync(() =>
                                            {
                                                var media = new Media(Main.LibVlc, file.OriginalPath);
                                                media.Parse();
                                                MediaPlayer fakePlayer = new(media) { Mute = true, Volume = 0 };
                                                uint w = 0, h = 0;
                                                fakePlayer.Size(0, ref w, ref h);
                                                file.Labels.Add(h >= CurrentSettings.AutomateImageSizeMinHeight &&
                                                                w >= CurrentSettings.AutomateImageSizeMinWidth
                                                    ? bigger
                                                    : smaller);
                                            });
                                            break;
                                        }
                                        default:
                                            file.Labels.Add(unknown);
                                            break;
                                    }

                                    break;
                            }

                            file.TargetFile(CurrentSettings);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(Lang.Lang.AutoSort_ErrorOnFile.Replace("$file_path$", files[i].OriginalPath)
                                .Replace("$ex$", ex.ToString()));
                        }

                        status.Percentage = i * 100 / files.Length;

                        Thread.Sleep(10);
                    }

                    if (files.Length <= 0)
                        errors.Add(Lang.Lang.Error_NoFilesLoaded);

                    if (errors.Count > 0)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main?.ShowControl(new ErrorScreen()
                                .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main?.ShowControl(this)))
                                .ReturnBackTo(this));
                        });
                        return;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Main?.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                            .ReturnBackTo(this));
                    });
                    break;
                }
            }
        }).ReturnBackTo(this));
    }

    private void ShowAbout(object? sender, RoutedEventArgs e)
    {
        Main?.ShowControl(new AboutScreen().ReturnBackTo(this));
    }

    private void AutomateFileTypeUseExt_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            CurrentSettings.AutomateFileTypeUseExtensions = ts.IsChecked is true;
    }

    private void ThemeSystemDefaultOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || sender is not RadioButton rb) return;
        app.RequestedThemeVariant =
            rb.IsChecked is true ? ThemeVariant.Default : app.RequestedThemeVariant;
        Main?.SetOpacity(UseBlur.IsChecked is true);
    }

    private void ThemeLightOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || sender is not RadioButton rb) return;
        app.RequestedThemeVariant = rb.IsChecked is true ? ThemeVariant.Light : app.RequestedThemeVariant;
        Main?.SetOpacity(UseBlur.IsChecked is true);
    }

    private void ThemeDarkOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || sender is not RadioButton rb) return;
        app.RequestedThemeVariant = rb.IsChecked is true ? ThemeVariant.Dark : app.RequestedThemeVariant;
        Main?.SetOpacity(UseBlur.IsChecked is true);
    }

    private void UseBlurOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        Main?.SetOpacity(UseBlur.IsChecked is true);
    }
}