using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class StartPage : LUC
{
    public StartPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Main is null) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            for (var i = Main.Settings.LastItems.Length - 1; i >= 0; i--)
                LastOpened.Children.Add(GenerateItem(Main.Settings.LastItems[i]));
        });
    }

    private Control GenerateItem(SettingsItem item)
    {
        var content = new DockPanel { LastChildFill = true, Margin = new Thickness(5) };
        var content1 = new StackPanel
            { Spacing = 5, Orientation = Orientation.Vertical };

        var icon = new PathIcon
        {
            Margin = new Thickness(0, 0, 5, 0),
            [!ForegroundProperty] = HistoryItem[!ForegroundProperty],
            [!PathIcon.DataProperty] = HistoryItem[!PathIcon.DataProperty]
        };
        DockPanel.SetDock(icon, Dock.Left);
        content.Children.Add(icon);
        content.Children.Add(content1);

        var content2 = new DockPanel
        {
            LastChildFill = true
        };
        var title = new TextBlock
        {
            Text = item.FileName, FontSize = 17.5, FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var date = new TextBlock
        {
            Text = item.LastOpened.ToString("f"),
            FontSize = 12.5,
            FontWeight = FontWeight.Light,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(date, Dock.Right);
        content2.Children.Add(date);
        content2.Children.Add(title);

        content1.Children.Add(content2);

        content1.Children.Add(new Separator());
        content1.Children.Add(new TextBlock
        {
            Text = item.Path,
            TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 12.5,
            FontWeight = FontWeight.Light
        });

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch, Content = content,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        button.Click += (_, _) =>
        {
            Main?.ShowControl(new ImportPage().WithFile(item.Path).ReturnBackTo(this));
            item.LastOpened = DateTime.Now;
            date.Text = item.LastOpened.ToString("f");
        };
        return button;
    }

    private void NewProject(object? sender, RoutedEventArgs e)
    {
        Main?.ShowControl(new NewLabelProject().ReturnBackTo(this));
    }

    private void ImportProject(object? sender, RoutedEventArgs e)
    {
        Main?.ShowControl(new ImportPage().ReturnBackTo(this));
    }

    private void ClearLastOpened(object? sender, RoutedEventArgs e)
    {
        if (Main is not null) Main.Settings.LastItems = Array.Empty<SettingsItem>();
        LastOpened?.Children.Clear();
    }
}