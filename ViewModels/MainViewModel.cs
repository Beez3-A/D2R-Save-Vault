using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using D2RSaveVault.Models;
using D2RSaveVault.Mvvm;
using D2RSaveVault.Services;

namespace D2RSaveVault.ViewModels;

public enum AppSection
{
    Backups,
    Options,
    About
}

public sealed class MainViewModel : ObservableObject
{
    private readonly BackupService _backup;
    private readonly DispatcherTimer _d2rLifecycleTimer;
    private AppSection _selectedSection = AppSection.Backups;
    private bool _d2rWasRunning;

    private const string StartupRegistryName = "D2R Save Vault";
    private static readonly string[] LegacyStartupRegistryNames =
    {
        "D2R Infernal Companion",
        "D2R Save Sentinel"
    };

    public AppSettings Settings => App.Settings;
    public BackupService Backup => _backup;
    public Array BackupIntervalUnits { get; } = Enum.GetValues<BackupIntervalUnit>();

    public AppSection SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public RelayCommand NavigateCommand { get; }

    public MainViewModel()
    {
        _backup = new BackupService(Settings);

        NavigateCommand = new RelayCommand(p =>
        {
            if (p is AppSection s)
                SelectedSection = s;
            else if (p is string str && Enum.TryParse<AppSection>(str, out var parsed))
                SelectedSection = parsed;
        });

        if (Settings.LaunchWithWindows)
            TrySetLaunchWithWindows(true, out _);

        _d2rWasRunning = D2RProcessDetector.IsRunning();
        _backup.InitializeAutomaticBackups(_d2rWasRunning, Settings.BindToD2R);

        _d2rLifecycleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _d2rLifecycleTimer.Tick += (_, _) => CheckD2RLifecycle();
        _d2rLifecycleTimer.Start();
    }

    public bool ToggleAutomaticBackups()
    {
        Settings.AutoStartBackups = !Settings.AutoStartBackups;
        Settings.Save();
        return _backup.SyncAutomaticBackups(
            D2RProcessDetector.IsRunning(),
            Settings.BindToD2R,
            logChange: true);
    }

    public bool OnAutomaticBackupsSettingChanged()
    {
        Settings.Save();
        return _backup.SyncAutomaticBackups(
            D2RProcessDetector.IsRunning(),
            Settings.BindToD2R,
            logChange: true);
    }

    public Task<bool> BackupNowAsync() => _backup.PerformBackupAsync();

    public Task<bool> RestoreBackupAsync(string backupName) => _backup.RestoreAsync(backupName);

    public void ApplyBackupIntervalSettings()
    {
        Settings.Save();
        _backup.ApplyIntervalSettings(D2RProcessDetector.IsRunning(), Settings.BindToD2R);
    }

    public void OnBackupPathChanged()
    {
        Settings.Save();
        _backup.OnSavePathChanged(D2RProcessDetector.IsRunning(), Settings.BindToD2R);
    }

    public void OnBackupFolderChanged()
    {
        Settings.Save();
        _backup.OnBackupFolderChanged(D2RProcessDetector.IsRunning(), Settings.BindToD2R);
    }

    public bool TrySetLaunchWithWindows(bool enabled, out string? error)
    {
        error = null;
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (runKey == null)
                throw new InvalidOperationException("Windows startup registry key could not be opened.");

            if (enabled)
            {
                string exePath = Environment.ProcessPath
                                 ?? throw new InvalidOperationException("The application path could not be determined.");
                runKey.SetValue(StartupRegistryName, $"\"{exePath}\"");
                foreach (string legacyName in LegacyStartupRegistryNames)
                    runKey.DeleteValue(legacyName, throwOnMissingValue: false);
            }
            else
            {
                runKey.DeleteValue(StartupRegistryName, throwOnMissingValue: false);
                foreach (string legacyName in LegacyStartupRegistryNames)
                    runKey.DeleteValue(legacyName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Logger.Log(ex);
            return false;
        }
    }

    public void OnLaunchWithWindowsChanged(bool wanted)
    {
        if (TrySetLaunchWithWindows(wanted, out string? error))
        {
            Settings.LaunchWithWindows = wanted;
            Settings.Save();
            return;
        }

        Settings.LaunchWithWindows = !wanted;
        Settings.Save();
        MessageBox.Show(
            "Windows could not update the startup setting.\n\n" + error,
            "Startup setting",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void OnBindSettingChanged()
    {
        Settings.Save();
        bool running = D2RProcessDetector.IsRunning();
        _d2rWasRunning = running;
        _backup.SyncAutomaticBackups(running, Settings.BindToD2R, logChange: true);
    }

    private void CheckD2RLifecycle()
    {
        bool running = D2RProcessDetector.IsRunning();
        if (running == _d2rWasRunning)
            return;

        _d2rWasRunning = running;
        _backup.HandleD2RStateChanged(running, Settings.BindToD2R);
    }

    public void Shutdown()
    {
        _d2rLifecycleTimer.Stop();
        _backup.Dispose();
        Settings.Save();
    }
}
