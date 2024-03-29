using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using File = System.IO.File;

namespace BlueLabel.Views;

public partial class PreviewVideo : PreviewUC
{
    private string _file = string.Empty;

    public PreviewVideo()
    {
        InitializeComponent();
    }

    public override PreviewUC LoadWithFile(string file)
    {
        _file = file;
        return this;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_file) || !File.Exists(_file) || Main is null) return;

        VideoView1.MediaPlayer = new MediaPlayer(new Media(Main.LibVlc,
            new StreamMediaInput(new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))));

        VideoView1.MediaPlayer.EndReached += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Repeat.IsChecked is not true) return;
                VideoView1.MediaPlayer.Position = 0;
                VideoView1.MediaPlayer.Play();
            });
        };

        FullPos.Text = TimeSpan.FromMilliseconds(VideoView1.MediaPlayer.Length).ToString("g");

        VideoView1.MediaPlayer.PositionChanged += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentPos.Text = TimeSpan
                    .FromMilliseconds(VideoView1.MediaPlayer.Length * VideoView1.MediaPlayer.Position)
                    .ToString("g");
                PosSlider.IsEnabled = false;
                PosSlider.Value = PosSlider.Maximum * VideoView1.MediaPlayer.Position;
                PosSlider.IsEnabled = true;
            });
        };
    }

    private void Volume_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSplitButton tb || VideoView1?.MediaPlayer is null) return;
        VideoView1.MediaPlayer.Mute = tb.IsChecked;
    }

    private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || VideoView1?.MediaPlayer is null) return;
        VideoView1.MediaPlayer.Volume = (int)slider.Value;
    }

    private void PosSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider { IsEnabled: true } slider || VideoView1?.MediaPlayer is null) return;
        VideoView1.MediaPlayer.Position = (float)(slider.Value / slider.Maximum);
    }

    private void Stop_OnClick(object? sender, RoutedEventArgs e)
    {
        VideoView1.MediaPlayer?.Stop();
        PlayPause.IsEnabled = false;
        PlayPause.IsChecked = false;
        PlayPause.IsEnabled = true;

        PosSlider.IsEnabled = false;
        PosSlider.Value = 0;
        PosSlider.IsEnabled = true;
    }

    private void PlayPause_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { IsEnabled: true } tb || VideoView1?.MediaPlayer is null) return;
        if (tb.IsChecked is true) VideoView1.MediaPlayer.Play();
        else VideoView1.MediaPlayer.Pause();
    }

    private void FastForward_OnClick(object? sender, RoutedEventArgs e)
    {
        if (PosSlider.Value < PosSlider.Maximum)
            PosSlider.Value += .01 * PosSlider.Maximum;
    }

    private void Rewind_OnClick(object? sender, RoutedEventArgs e)
    {
        if (PosSlider.Value > 0)
            PosSlider.Value -= .01 * PosSlider.Maximum;
    }
}