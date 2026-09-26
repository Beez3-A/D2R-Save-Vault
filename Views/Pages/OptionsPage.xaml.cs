using System.Windows;
using System.Windows.Controls;
using D2RSaveVault.Models;
using D2RSaveVault.ViewModels;

namespace D2RSaveVault.Views;

public partial class OptionsPage : UserControl
{
    public OptionsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        ThemeCombo.SelectedIndex = Vm.Settings.Theme switch
        {
            AppTheme.Light => 1,
            AppTheme.System => 2,
            _ => 0
        };
    }

    private void Save_OnChanged(object sender, RoutedEventArgs e) => Vm?.Settings.Save();

    private void LaunchWithWindows_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || Vm == null)
            return;
        Vm.OnLaunchWithWindowsChanged(Vm.Settings.LaunchWithWindows);
    }

    private void Bind_OnChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Vm?.OnBindSettingChanged();
    }

    private void Theme_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Vm == null || !IsLoaded)
            return;

        var theme = ThemeCombo.SelectedIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.System,
            _ => AppTheme.Dark
        };
        Vm.Settings.Theme = theme;
        App.ApplyTheme(theme);
        Vm.Settings.Save();
    }

    private void Reset_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        var confirm = MessageBox.Show(
            Window.GetWindow(this),
            "Restore the tray, Windows startup, D2R binding, and theme options to their defaults?\n\nYour save folder, backup folder, interval, retention limit, and existing backups will not be changed.",
            "Reset app options",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        Vm.TrySetLaunchWithWindows(false, out _);
        Vm.Settings.ResetAppDefaults();
        ThemeCombo.SelectedIndex = 0;
        App.ApplyTheme(Vm.Settings.Theme);
        Vm.OnBindSettingChanged();
        Vm.Settings.Save();
    }
}
