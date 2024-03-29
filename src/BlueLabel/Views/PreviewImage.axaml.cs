using System.IO;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace BlueLabel.Views;

public partial class PreviewImage : PreviewUC
{
    private Bitmap? img;

    public PreviewImage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (img is null) return;
            ImageToPreview.Source = img;
            ImageInfo.Text =
                $"{img.Size.Width}" +
                $" x " +
                $"{img.Size.Height}" +
                $" - " +
                $"{img.Dpi} dpi" +
                $" - " +
                $"{img.PixelSize.Width}" +
                $" x " +
                $"{img.PixelSize.Height} " +
                $"({img.PixelSize.AspectRatio})" +
                $" - " +
                $"{(img.Format.HasValue ? img.Format.Value.BitsPerPixel : "??")} bpp ";
        };
    }

    public override PreviewUC LoadWithFile(string file)
    {
        if (File.Exists(file)) img = new Bitmap(file);
        return this;
    }

    private void ViewMode_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb || img is null) return;
        ImageToPreview.Width = tb.IsChecked is true ? ImageBorder.Bounds.Width : img.Size.Width;
        ImageToPreview.Width = tb.IsChecked is true ? ImageBorder.Bounds.Height : img.Size.Height;
    }
}