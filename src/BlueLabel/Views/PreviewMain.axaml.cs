using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace BlueLabel.Views;

public partial class PreviewMain : LUC
{
    private int CurrentPos;
    private LabelerSetting? CurrentSetting;
    private LabelFile[] Files = Array.Empty<LabelFile>();

    public PreviewMain()
    {
        InitializeComponent();
#if DEBUG
        if (Design.IsDesignMode)
            ContentPanel.Children.Add(new PreviewText().LoadWithText(Lang.Lang.Preview_PreviewMode));
#endif
    }

    private LabelFile CurrentItem => Files[CurrentPos];

    public PreviewMain WithListAndSettings(LabelerSetting setting, LabelFile[] files)
    {
        CurrentSetting = setting;
        Files = files;
        return this;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (CurrentSetting is null) return;

        if (Files.Length <= 0)
        {
            FinishButton.IsEnabled = false;
            PreviousItemButton.IsEnabled = false;
            NextItemButton.IsEnabled = false;
            AddNewLabel.IsEnabled = false;
            CurrentView.IsChecked = true;
            CurrentView.IsEnabled = false;
            FileInfo.Text = Lang.Lang.Error_NoFilesLoaded;
            return;
        }

        List<Label> labels = new();

        foreach (var file in Files)
        foreach (var label in file.Labels)
            if (labels.FindAll(it => string.Equals(it.Name, label.Name)).Count <= 0)
                labels.Add(label);

        foreach (var label in labels)
        {
            ToggleButton button = new() { Content = label.Name, Tag = label };
            button.IsCheckedChanged += LabelButtonClick;
            Labels.Children.Add(button);
        }

        ShowItem(CurrentItem);
    }

    private void AddLabel(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LabelName.Text)) return;
        ToggleButton button = new() { Content = LabelName.Text, Tag = new Label(LabelName.Text) };
        button.IsCheckedChanged += LabelButtonClick;
        Labels.Children.Add(button);
    }

    private void LabelButtonClick(object? s, RoutedEventArgs e)
    {
        if (s is not ToggleButton { IsEnabled: true, Tag: Label label, Parent: Control { IsEnabled: true } } tb) return;
        switch (tb.IsChecked)
        {
            case true when !CurrentItem.Labels.Contains(label):
                CurrentItem.Labels.Add(label);
                break;
            case false when CurrentItem.Labels.Contains(label):
                CurrentItem.Labels.Remove(label);
                break;
        }

        if (CurrentSetting is null) return;
        if (CurrentSetting.AllowRecursiveSubfolders ||
            CurrentSetting.LabelFilesBy != LabelFilesOptions.Subfolder) return;
        Labels.IsEnabled = false;
        foreach (var item in Labels.Children)
            if (item != tb && item is ToggleButton tb_others)
                tb_others.IsChecked = false;

        Labels.IsEnabled = true;
    }

    private void ShowItem(LabelFile file)
    {
        ContentPanel.Children.Clear();
        try
        {
            Control preview = file.Type switch
            {
                FileType.Unsupported => new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = Lang.Lang.Preview_FileNotSupported
                },
                FileType.Archive => new PreviewArchive { Main = Main }.LoadWithFile(file.OriginalPath),
                FileType.Audio => new PreviewAudio { Main = Main }.LoadWithFile(file.OriginalPath),
                FileType.Image => new PreviewImage { Main = Main }.LoadWithFile(file.OriginalPath),
                FileType.Video => new PreviewVideo { Main = Main }.LoadWithFile(file.OriginalPath),
                FileType.Text => new PreviewText { Main = Main }.LoadWithFile(file.OriginalPath),
                _ => new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = Lang.Lang.Preview_FileNotSupported
                }
            };
            ContentPanel.Children.Add(preview);
        }
        catch (Exception ex)
        {
            ContentPanel.Children.Add(new SelectableTextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = Lang.Lang.Preview_ErrorWhilePreviewing.Replace("$ex$", ex.ToString())
            });
        }

        var info = new FileInfo(file.OriginalPath);
        FileInfo.Text = Lang.Lang.Preview_FileInfo
            .Replace("$newline$", Environment.NewLine)
            .Replace("$file_name$", file.OriginalFileName)
            .Replace("$file_type$", file.Type.ToString())
            .Replace("$file_ex$", file.FileExtension)
            .Replace("$file_path$", file.OriginalPath)
            .Replace("$file_size$", SimplifyFileSize(info.Length))
            .Replace("$file_size_bytes$", "" + info.Length)
            .Replace("$file_links_to$", info.LinkTarget ?? Lang.Lang.PreviewMain_FileInfo_LinksToNothing)
            .Replace("$file_read_only$", info.IsReadOnly ? Lang.Lang.PreviewMain_FileInfo_ReadOnly : "")
            .Replace("$file_creation_time$", info.CreationTime.ToString("f"))
            .Replace("$file_last_access$", info.LastAccessTime.ToString("f"))
            .Replace("$file_last_write$", info.LastWriteTime.ToString("f"))
            .Replace("$file_unix_mode$", info.UnixFileMode.ToString())
            .Replace("$file_attr$", info.Attributes.ToString());

        Labels.IsEnabled = false;

        foreach (var label in file.Labels)
        foreach (var item in Labels.Children)
            if (item is ToggleButton tb)
                tb.IsChecked = tb.Tag == label;

        Labels.IsEnabled = true;


        PreviousItemButton.IsEnabled = CurrentPos != 0;
        NextItemButton.IsEnabled = CurrentPos < Files.Length - 1;
        FinishButton.IsEnabled = CurrentPos >= Files.Length - 1;
    }

    private static string SimplifyFileSize(long fileSize)
    {
        const long KiB = 1024;
        const long MiB = KiB * 1024;
        const long GiB = MiB * 1024;
        const long TiB = GiB * 1024;

        return fileSize switch
        {
            < KiB => fileSize + " bytes",
            < MiB => Math.Round((double)fileSize / KiB, 2) + " KiB",
            < GiB => Math.Round((double)fileSize / MiB, 2) + " MiB",
            < TiB => Math.Round((double)fileSize / GiB, 2) + " GiB",
            _ => Math.Round((double)fileSize / TiB, 2) + " TiB"
        };
    }

    private void PreviousItemButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { IsEnabled : true }) return;
        CurrentPos--;
        ShowItem(CurrentItem);
    }

    private void NextItemButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { IsEnabled : true }) return;
        CurrentPos++;
        ShowItem(CurrentItem);
    }

    private void FinishLabeling(object? sender, RoutedEventArgs e)
    {
        if (CurrentSetting is null) return;
        foreach (var file in Files) file.TargetFile(CurrentSetting);
        Main?.ShowControl(new ConfirmScreen().WithListAndSettings(CurrentSetting, Files).ReturnBackTo(this));
    }

    private void GoBack(object? sender, RoutedEventArgs e)
    {
        Main?.ShowControl(ReturnTo ?? new NewLabelProject());
    }
}