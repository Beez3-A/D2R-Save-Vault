using System.ComponentModel;
using System.Windows;
using D2RSaveVault.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using Wpf.Ui.Controls;

namespace D2RSaveVault.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _vm;
    private bool _reallyExit;
    private bool _shutdownDone;
    private bool _launchTrayHandled;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        StateChanged += MainWindow_OnStateChanged;
        ContentRendered += MainWindow_OnContentRendered;
    }

    private void MainWindow_OnContentRendered(object? sender, EventArgs e)
    {
        if (_launchTrayHandled)
            return;
        _launchTrayHandled = true;
        if (_vm.Settings.LaunchMinimized)
            HideToTray(showHint: false);
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _vm.Settings.MinimizeToTray)
            Dispatcher.BeginInvoke(() => HideToTray(showHint: false));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_reallyExit && _vm.Settings.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            HideToTray(showHint: true);
            return;
        }

        _reallyExit = true;
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_shutdownDone)
        {
            _shutdownDone = true;
            _vm.Shutdown();
            Tray.Dispose();
        }

        base.OnClosed(e);
        Application.Current.Shutdown();
    }

    private void HideToTray(bool showHint)
    {
        if (_reallyExit)
            return;

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Hide();

        if (showHint && !_vm.Settings.TrayHintShown)
        {
            _vm.Settings.TrayHintShown = true;
            _vm.Settings.Save();
            Tray.ShowBalloonTip(
                "Still running",
                "D2R Save Vault is still running in the system tray. If backups are bound to D2R, it will wait for the game and back up only while D2R is open.",
                BalloonIcon.Info);
        }
    }

    private void Tray_OnDoubleClick(object sender, RoutedEventArgs e) => ShowFromTray();
    private void TrayOpen_OnClick(object sender, RoutedEventArgs e) => ShowFromTray();

    private async void TrayBackupNow_OnClick(object sender, RoutedEventArgs e)
        => await _vm.BackupNowAsync();

    private void TrayExit_OnClick(object sender, RoutedEventArgs e)
    {
        _reallyExit = true;
        Close();
    }

    private void ShowFromTray()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsVisible)
                Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }
}
