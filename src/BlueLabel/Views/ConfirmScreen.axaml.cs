using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class ConfirmScreen : LUC
{
    private LabelerSetting? CurrentSetting;
    private LabelFile[] Files = Array.Empty<LabelFile>();

    public ConfirmScreen()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            foreach (var file in Files) LabeledFiles.Children.Add(GenerateEntry(file));
        };
    }

    private Control GenerateEntry(LabelFile file)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ]
        };
        TextBox tbOriginal = new() { IsReadOnly = true, Text = file.OriginalPath };
        Grid.SetColumn(tbOriginal, 0);
        grid.Children.Add(tbOriginal);

        TextBlock arrow = new() { VerticalAlignment = VerticalAlignment.Center, Text = "->" };
        Grid.SetColumn(arrow, 1);
        grid.Children.Add(arrow);

        TextBox tbTarget = new() { Text = file.FinalTargetFile };
        Grid.SetColumn(tbTarget, 2);
        tbTarget.TextChanged += (_, _) => file.FinalTargetFile = tbTarget.Text;
        grid.Children.Add(tbTarget);

        Button browseButton = new() { Content = "..." };
        Grid.SetColumn(browseButton, 3);
        grid.Children.Add(browseButton);

        browseButton.Click += async (_, _) =>
        {
            await Task.Run(async () =>
            {
                if (Main?.GetStorageProvider() is not { CanPickFolder: true } storage || CurrentSetting is null) return;

                var target_file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = Lang.Lang.ConfirmScreen_SelectOutputFile,
                    SuggestedStartLocation =
                        await storage.TryGetFolderFromPathAsync(file.FinalTargetFile ?? file.TargetFile(CurrentSetting))
                });

                if (target_file is null) return;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    tbTarget.Text = target_file.Path.AbsolutePath;
                    file.FinalTargetFile = target_file.Path.AbsolutePath;
                });
            });
        };

        return grid;
    }

    public ConfirmScreen WithListAndSettings(LabelerSetting? setting, LabelFile[] files)
    {
        CurrentSetting = setting;
        Files = files;
        return this;
    }

    private void Back(object? sender, RoutedEventArgs e)
    {
        Main?.ShowControl(ReturnTo ?? new StartPage());
    }

    private void Continue(object? sender, RoutedEventArgs e)
    {
        if (CurrentSetting is null || Files.Length <= 0) return;
        Main?.ShowControl(new LoadingScreen().WithAction(status =>
        {
            status.Title = CurrentSetting.Operation switch
            {
                OperationType.Copy => Lang.Lang.Status_CopyingFiles,
                OperationType.Move => Lang.Lang.Status_MovingFiles,
                _ => Lang.Lang.Status_IHaveNoIdea
            };
            List<string> errors = new();
            for (var i = 0; i < Files.Length; i++)
            {
                var file = Files[i];
                status.WorkingOn = file.OriginalFileName;
                try
                {
                    var dest = file.FinalTargetFile ?? file.TargetFile(CurrentSetting);
                    new FileInfo(dest).Directory?.Create();
                    switch (CurrentSetting.Operation)
                    {
                        case OperationType.Copy:
                            File.Copy(file.OriginalPath, dest, true);
                            break;

                        case OperationType.Move:
                            File.Move(file.OriginalPath, dest, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add((CurrentSetting.Operation == OperationType.Copy
                            ? Lang.Lang.Status_ErrorCopyingExceptionCuaght
                            : Lang.Lang.Status_ErrorMovingExceptionCaught).Replace("$file_name$", file.OriginalPath)
                        .Replace("$ex$", ex.ToString()));
                }


                status.Percentage = i * 100 / Files.Length;
            }

            if (errors.Count > 0)
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Main?.ShowControl(
                        new ErrorScreen().WithError(string.Join(Environment.NewLine, errors.ToArray())));
                });
            else
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (AutoCloseApp.IsChecked is true)
                    {
                        Main?.Close();
                    }
                    else
                    {
                        status.Percentage = 100;
                        status.Title = Lang.Lang.Status_Finished;
                        status.WorkingOn = Lang.Lang.WorkingOn_Nothing;
                    }
                });
        }).ReturnBackTo(this));
    }
}