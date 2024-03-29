using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace BlueLabel.Views;

public partial class MainView : UserControl
{
    private bool _cleanupRunning;
    public LibVLC LibVlc = new();

    public MainView()
    {
        InitializeComponent();
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

    internal void SetOpacity(bool enabled)
    {
        if (Parent is not MainWindow mw) return;
        mw.SetOpacity(enabled);
    }

    private void SetVisuals(object? data)
    {
        DataContext = data;
        foreach (Control? control in ContentCarousel.Items)
            if (control is not null)
                control.DataContext = data;
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
}