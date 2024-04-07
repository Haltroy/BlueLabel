using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BlueLabel.Views;
using LibVLCSharp.Shared;

namespace BlueLabel;

internal static class Tools
{
    public static void SetAccent(this Application app, Color color)
    {
        app.Resources["SystemAccentColor"] = color;
        app.Resources["SystemAccentColorDark1"] = color;
        app.Resources["SystemAccentColorDark2"] = ShiftBrightness(color, 20);
        app.Resources["SystemAccentColorDark3"] = ShiftBrightness(color, 40);
        app.Resources["SystemAccentColorLight1"] = color;
        app.Resources["SystemAccentColorLight2"] = ShiftBrightness(color, 20);
        app.Resources["SystemAccentColorLight3"] = ShiftBrightness(color, 40);
    }

    private static Color ShiftBrightness(Color c, int value, bool shiftAlpha = false)
    {
        return new Color(
            shiftAlpha
                ? !IsTransparencyHigh(c)
                    ? (byte)AddIfNeeded(c.A, value, byte.MaxValue)
                    : (byte)SubtractIfNeeded(c.A, value)
                : c.A,
            !IsBright(c) ? (byte)AddIfNeeded(c.R, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.R, value),
            !IsBright(c) ? (byte)AddIfNeeded(c.G, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.G, value),
            !IsBright(c) ? (byte)AddIfNeeded(c.B, value, byte.MaxValue) : (byte)SubtractIfNeeded(c.B, value));
    }

    private static int Brightness(Color c)
    {
        return (int)Math.Sqrt(
            c.R * c.R * .241 +
            c.G * c.G * .691 +
            c.B * c.B * .068);
    }

    private static bool IsTransparencyHigh(Color c)
    {
        return c.A < 130;
    }

    private static bool IsBright(Color c)
    {
        return Brightness(c) > 130;
    }

    private static int SubtractIfNeeded(int number, int subtract, int limit = 0)
    {
        return limit == 0 ? number > subtract ? number - subtract : number :
            number - subtract < limit ? number : number - subtract;
    }

    private static int AddIfNeeded(int number, int add, int limit = int.MaxValue)
    {
        return number + add > limit ? number : number + add;
    }

    public static string ToXML(this string s)
    {
        return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    public static Action<LoadingStatus> Automation(LabelerSetting CurrentSettings, MainView Main, LUC sender)
    {
        return status =>
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
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Main.ShowControl(new ErrorScreen().WithError(string.Join(Environment.NewLine, errors.ToArray()))
                        .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main.ShowControl(sender)))
                        .ReturnBackTo(sender));
                });
                return;
            }

            // We grouped them into one single section below because they all use same labels.
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (CurrentSettings.Automation)
            {
                case AutomationType.Manual:
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Main.ShowControl(new PreviewMain().WithListAndSettings(CurrentSettings, files)
                            .ReturnBackTo(sender));
                    });
                    break;
                case AutomationType.AutomateByFileType:
                {
                    status.Title = Lang.Lang.AutoSort_Title;
                    status.IsIndeterminate = false;
                    if (CurrentSettings.AutomateFileTypeUseExtensions)
                    {
                        List<Label> labels = [];

                        for (var i = 0; i < files.Length; i++)
                        {
                            try
                            {
                                status.WorkingOn = files[i].OriginalFileName;

                                var i1 = i;
                                if (labels.FindAll(it =>
                                        string.Equals(it.Name, files[i1].FileExtension.Substring(1))) is
                                    { Count: > 0 } list)
                                {
                                    files[i].Labels.AddRange(list.ToArray());
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
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                Main.ShowControl(new ErrorScreen()
                                    .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                    .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main.ShowControl(sender)))
                                    .ReturnBackTo(sender));
                            });
                            return;
                        }

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                                .ReturnBackTo(sender));
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
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                Main.ShowControl(new ErrorScreen()
                                    .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                    .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main.ShowControl(sender)))
                                    .ReturnBackTo(sender));
                            });
                            return;
                        }

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                                .ReturnBackTo(sender));
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
                                        Dispatcher.UIThread.InvokeAsync(() =>
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
                                            Dispatcher.UIThread.InvokeAsync(() =>
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
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Main.ShowControl(new ErrorScreen()
                                .WithError(string.Join(Environment.NewLine, errors.ToArray()))
                                .WithContinue(() => Dispatcher.UIThread.InvokeAsync(() => Main.ShowControl(sender)))
                                .ReturnBackTo(sender));
                        });
                        return;
                    }

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Main.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSettings, files)
                            .ReturnBackTo(sender));
                    });
                    break;
                }
            }
        };
    }
}