using System.IO;
using D2RSaveVault.Mvvm;
using Newtonsoft.Json;

namespace D2RSaveVault.Models;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public enum BackupIntervalUnit
{
    Seconds,
    Minutes,
    Hours
}

/// <summary>
/// Persisted settings for D2R Save Vault.
/// </summary>
public sealed class AppSettings : ObservableObject
{
    [JsonIgnore] private string? _filePath;

    private bool _launchMinimized = true;
    private bool _minimizeToTray = true;
    private bool _minimizeToTrayOnClose = true;
    private bool _launchWithWindows;
    private bool _bindToD2R = true;
    private AppTheme _theme = AppTheme.Dark;

    // Kept under the original JSON property name for seamless migration from
    // previous combined-app backup settings.
    private bool _autoStartBackups = true;
    private string _backupSavePath = string.Empty;
    private string _backupFolderPath = string.Empty;
    private int _backupIntervalValue = 1;
    private BackupIntervalUnit _backupIntervalUnit = BackupIntervalUnit.Minutes;
    private int _maxBackups = 5;

    /// <summary>Start the app hidden in the notification area.</summary>
    public bool LaunchMinimized
    {
        get => _launchMinimized;
        set => SetProperty(ref _launchMinimized, value);
    }

    /// <summary>Hide to the tray when the main window is minimized.</summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    /// <summary>Hide to the tray when the user clicks X.</summary>
    public bool MinimizeToTrayOnClose
    {
        get => _minimizeToTrayOnClose;
        set => SetProperty(ref _minimizeToTrayOnClose, value);
    }

    public bool LaunchWithWindows
    {
        get => _launchWithWindows;
        set => SetProperty(ref _launchWithWindows, value);
    }

    /// <summary>
    /// When enabled, automatic backups run only while Diablo II: Resurrected is running.
    /// </summary>
    public bool BindToD2R
    {
        get => _bindToD2R;
        set => SetProperty(ref _bindToD2R, value);
    }

    /// <summary>
    /// Master switch for scheduled backups. The legacy property name is retained
    /// so existing settings migrate without losing the user's preference.
    /// </summary>
    public bool AutoStartBackups
    {
        get => _autoStartBackups;
        set => SetProperty(ref _autoStartBackups, value);
    }

    public string BackupSavePath
    {
        get => _backupSavePath;
        set => SetProperty(ref _backupSavePath, value ?? string.Empty);
    }

    /// <summary>Optional custom folder in which full backup folders are stored.</summary>
    public string BackupFolderPath
    {
        get => _backupFolderPath;
        set => SetProperty(ref _backupFolderPath, value ?? string.Empty);
    }

    public int BackupIntervalValue
    {
        get => _backupIntervalValue;
        set => SetProperty(ref _backupIntervalValue, Math.Clamp(value, 1, 2_000_000));
    }

    public BackupIntervalUnit BackupIntervalUnit
    {
        get => _backupIntervalUnit;
        set => SetProperty(ref _backupIntervalUnit, value);
    }

    public int MaxBackups
    {
        get => _maxBackups;
        set => SetProperty(ref _maxBackups, Math.Clamp(value, 1, 100));
    }

    /// <summary>Whether the one-time tray notice has already been shown.</summary>
    public bool TrayHintShown { get; set; }

    public AppTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public void UpdatePath(string path) => _filePath = path;

    /// <summary>
    /// Reset general app behaviour without changing save paths, backup interval,
    /// retention, or whether scheduled backups are enabled.
    /// </summary>
    public void ResetAppDefaults()
    {
        LaunchMinimized = true;
        MinimizeToTray = true;
        MinimizeToTrayOnClose = true;
        LaunchWithWindows = false;
        BindToD2R = true;
        Theme = AppTheme.Dark;
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(_filePath))
            return;

        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_filePath))
                File.Replace(tmp, _filePath, null);
            else
                File.Move(tmp, _filePath);
        }
        catch (Exception ex)
        {
            Services.Logger.Log(ex);
        }
    }

    public static AppSettings Load(string filePath)
    {
        AppSettings result;
        try
        {
            result = File.Exists(filePath)
                ? JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex)
        {
            Services.Logger.Log(ex);
            result = new AppSettings();
        }

        result._backupIntervalValue = Math.Clamp(result._backupIntervalValue, 1, 2_000_000);
        result._maxBackups = Math.Clamp(result._maxBackups, 1, 100);
        result._backupSavePath ??= string.Empty;
        result._backupFolderPath ??= string.Empty;
        result.UpdatePath(filePath);
        return result;
    }
}
