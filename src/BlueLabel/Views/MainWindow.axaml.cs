using Avalonia.Controls;
using Avalonia.Media;

namespace BlueLabel.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => SetOpacity(true);
    }

    internal void SetOpacity(bool enabled)
    {
        var opacity = enabled ? 0.5 : 1;
        switch (Background)
        {
            case SolidColorBrush scb:
                scb.Opacity = opacity;
                break;

            case VisualBrush vb:
                vb.Opacity = opacity;
                break;
            case ConicGradientBrush cgb:
                cgb.Opacity = opacity;
                break;
            case DrawingBrush db:
                db.Opacity = opacity;
                break;
            case ImageBrush ib:
                ib.Opacity = opacity;
                break;
            case TileBrush tb:
                tb.Opacity = opacity;
                break;
            case RadialGradientBrush rgb:
                rgb.Opacity = opacity;
                break;
            case GradientBrush gb:
                gb.Opacity = opacity;
                break;
        }
    }
}