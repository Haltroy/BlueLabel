using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace BlueLabel.Views;

public partial class MainView : UserControl
{
    public readonly LibVLC LibVlc = new();
    private bool _cleanupRunning;

    public MainView()
    {
        InitializeComponent();
    }

    public Settings Settings { get; } = new Settings().Load();

    private void ThemeSystemDefaultOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || UseBlur is null || sender is not RadioButton rb) return;
        app.RequestedThemeVariant =
            rb.IsChecked is true ? ThemeVariant.Default : app.RequestedThemeVariant;
        Settings.Theme = rb.IsChecked is true ? BlueLabel.Theme.Default : Settings.Theme;
        SetOpacity(UseBlur.IsChecked is true);
        Settings.Save();
    }

    private void ThemeLightOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || UseBlur is null || sender is not RadioButton rb) return;
        app.RequestedThemeVariant = rb.IsChecked is true ? ThemeVariant.Light : app.RequestedThemeVariant;
        Settings.Theme = rb.IsChecked is true ? BlueLabel.Theme.Light : Settings.Theme;
        SetOpacity(UseBlur.IsChecked is true);
        Settings.Save();
    }

    private void ThemeDarkOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app || UseBlur is null || sender is not RadioButton rb) return;
        app.RequestedThemeVariant = rb.IsChecked is true ? ThemeVariant.Dark : app.RequestedThemeVariant;
        Settings.Theme = rb.IsChecked is true ? BlueLabel.Theme.Dark : Settings.Theme;
        SetOpacity(UseBlur.IsChecked is true);
        Settings.Save();
    }

    private void UseBlurOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
            SetOpacity(ts.IsChecked is true);
    }

    internal IStorageProvider? GetStorageProvider()
    {
        return Parent is TopLevel topLevel ? topLevel.StorageProvider : null;
    }

    internal void Close()
    {
        switch (Application.Current?.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.Shutdown();
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new TextBlock { Text = Lang.Lang.CanSafelyCloseApp };
                break;
        }
    }

    private void SetOpacity(bool enabled)
    {
        if (Parent is not MainWindow mw) return;
        mw.SetOpacity(enabled);
        Settings.UseBlur = enabled;
        Settings.Save();
    }

    public void ShowControl(LUC control)
    {
        control.DataContext = DataContext;
        control.Main ??= this;
        CleanupCarousel(0);
        ContentCarousel.Items.Add(control);
        ContentCarousel.SelectedItem = control;
    }

    private async void CleanupCarousel(int sleep = 5000)
    {
        await Task.Run(async () =>
        {
            if (_cleanupRunning) return;
            _cleanupRunning = true;
            Thread.Sleep(sleep);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                for (var i = 0; i < ContentCarousel.Items.Count; i++)
                    if (ContentCarousel.SelectedItem != ContentCarousel.Items[i] &&
                        i != ContentCarousel.SelectedIndex - 1)
                        ContentCarousel.Items.Remove(ContentCarousel.Items[i]);
            });
            _cleanupRunning = false;
        });
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is not { } app) return;
        app.RequestedThemeVariant = Settings.Theme switch
        {
            BlueLabel.Theme.Default => ThemeVariant.Default,
            BlueLabel.Theme.Light => ThemeVariant.Light,
            BlueLabel.Theme.Dark => ThemeVariant.Dark,
            _ => app.RequestedThemeVariant
        };

        app.SetAccent(Settings.AccentColor);

        AccentColor.IsEnabled = false;
        AccentColor.Color = Settings.AccentColor;
        AccentColor.IsEnabled = true;

        if (app.RequestedThemeVariant == ThemeVariant.Default)
            ThemeSystemDefault.IsChecked = true;
        else if (app.RequestedThemeVariant == ThemeVariant.Light)
            ThemeLight.IsChecked = true;
        else if (app.RequestedThemeVariant == ThemeVariant.Dark)
            ThemeDark.IsChecked = true;

        VersionText.Text = "v"
                           + (
                               Assembly.GetExecutingAssembly() is { } ass
                               && ass.GetName() is { } name
                               && name.Version != null
                                   ? "" + (name.Version.Major > 0 ? name.Version.Major : "") +
                                     (name.Version.Minor > 0 ? "." + name.Version.Minor : "") +
                                     (name.Version.Build > 0 ? "." + name.Version.Build : "") +
                                     (name.Version.Revision > 0 ? "." + name.Version.Revision : "")
                                   : "?"
                           );
    }

    private void AccentColor_OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (sender is not ColorPicker { IsEnabled: true } picker || Application.Current is not { } app) return;
        app.SetAccent(picker.Color);
        Settings.AccentColor = picker.Color;
        Settings.Save();
    }

    private void ResetAccentColor(object? sender, RoutedEventArgs e)
    {
        if (AccentColor != null) AccentColor.Color = Settings.DefaultAccentColor;
    }
}