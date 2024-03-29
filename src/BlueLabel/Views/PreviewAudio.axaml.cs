using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using File = System.IO.File;

namespace BlueLabel.Views;

public partial class PreviewAudio : PreviewUC
{
    private string _file = string.Empty;
    private MediaPlayer? player;

    public PreviewAudio()
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

        player = new MediaPlayer(new Media(Main.LibVlc,
            new StreamMediaInput(new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))));

        player.EndReached += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Repeat.IsChecked is false) return;
                player.Stop();
                player.Play();
            });
        };

        player.PositionChanged += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FullPos.Text = TimeSpan.FromMilliseconds(player.Length).ToString("d\\.hh\\:mm\\:ss\\:ff");
                CurrentPos.Text = TimeSpan.FromMilliseconds(player.Length * player.Position)
                    .ToString("d\\.hh\\:mm\\:ss\\:ff");
                PosSlider.IsEnabled = false;
                PosSlider.Value = PosSlider.Maximum * player.Position;
                PosSlider.IsEnabled = true;
            });
        };

        var tagFile = TagLib.File.Create(_file);
        PreviewTitle.Text = tagFile.Tag.Title;
        PreviewArtist.Text = tagFile.Tag.AlbumArtists.Length > 0
            ? string.Join(',', tagFile.Tag.AlbumArtists)
            : tagFile.Tag.Album;

        if (tagFile.Tag.Pictures.Length > 0)
            TitleImage.Source = new Bitmap(new MemoryStream(tagFile.Tag.Pictures[0].Data.Data));
    }

    private void Volume_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSplitButton tb || player is null) return;
        player.Mute = tb.IsChecked;
    }

    private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || player is null) return;
        player.Volume = (int)slider.Value;
    }

    private void PosSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider { IsEnabled: true } slider || player is null) return;
        player.Position = (float)(slider.Value / slider.Maximum);
    }

    private void Stop_OnClick(object? sender, RoutedEventArgs e)
    {
        player?.Stop();
        PlayPause.IsEnabled = false;
        PlayPause.IsChecked = false;
        PlayPause.IsEnabled = true;

        PosSlider.IsEnabled = false;
        PosSlider.Value = 0;
        PosSlider.IsEnabled = true;
    }

    private void PlayPause_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { IsEnabled: true } tb || player is null) return;
        if (tb.IsChecked is true) player.Play();
        else player.Pause();
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