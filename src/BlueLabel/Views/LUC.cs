using Avalonia;
using Avalonia.Controls;

namespace BlueLabel.Views;

public class LUC : UserControl
{
    public static readonly StyledProperty<MainView?> MainProperty =
        AvaloniaProperty.Register<LUC, MainView?>(nameof(Main));

    internal LUC? ReturnTo;

    public MainView? Main
    {
        get => GetValue(MainProperty);
        set => SetValue(MainProperty, value);
    }

    public LUC ReturnBackTo(LUC luc)
    {
        ReturnTo = luc;
        return this;
    }
}