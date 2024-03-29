using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace BlueLabel.Views;

public partial class LoadingScreen : LUC
{
    private Action<LoadingStatus>? Status;

    public LoadingScreen()
    {
        InitializeComponent();
        var status = new LoadingStatus();
        status.OnStatusChanged += OnStatusChanged;
        Loaded += async (_, _) => await Task.Run(() =>
        {
            Thread.Sleep(5000);
            Status?.Invoke(status);
        });
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void OnStatusChanged(LoadingStatus status)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            WorkingOn.Text = status.WorkingOn;
            CurrentProgress.Value = status.Percentage;
            CurrentProgress.IsIndeterminate = status.IsIndeterminate;
            MainStatus.Text = status.Title;
        });
    }

    public LoadingScreen WithAction(Action<LoadingStatus> status)
    {
        Status = status;
        return this;
    }
}

public class LoadingStatus
{
    public delegate void OnStatusChangedDelegate(LoadingStatus status);

    private bool _indeterminate;
    private int _percentage;
    private string _title = string.Empty;
    private string _workingOn = string.Empty;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnStatusChanged?.Invoke(this);
        }
    }

    public string WorkingOn
    {
        get => _workingOn;
        set
        {
            _workingOn = value;
            OnStatusChanged?.Invoke(this);
        }
    }

    public int Percentage
    {
        get => _percentage;
        set
        {
            _percentage = value;
            OnStatusChanged?.Invoke(this);
        }
    }

    public bool IsIndeterminate
    {
        get => _indeterminate;
        set
        {
            _indeterminate = value;
            OnStatusChanged?.Invoke(this);
        }
    }

    public event OnStatusChangedDelegate? OnStatusChanged;
}