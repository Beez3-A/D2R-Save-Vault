using System.IO;
using System.Threading;
using System.Windows;
using D2RSaveVault.Models;
using D2RSaveVault.Services;
using D2RSaveVault.ViewModels;
using D2RSaveVault.Views;
using Wpf.Ui.Appearance;

namespace D2RSaveVault;

public partial class App : Application
{
    private const string MutexName = "D2RSaveVault_SingleInstance";
    private static Mutex? _mutex;

    public static AppSettings Settings { get; private set; } = new();
    public static string DataFolder { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "D2RSaveVault");
        Directory.CreateDirectory(DataFolder);

        Logger.Initialize(Path.Combine(DataFolder, "log.txt"));
        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Log(args.Exception);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

        string settingsPath = Path.Combine(DataFolder, "settings.json");
        MigrateLegacySettingsIfNeeded(settingsPath);
        Settings = AppSettings.Load(settingsPath);
        ApplyTheme(Settings.Theme);

        var mainViewModel = new MainViewModel();
        var window = new MainWindow(mainViewModel);
        MainWindow = window;
        window.Show();
    }

    private static void MigrateLegacySettingsIfNeeded(string newSettingsPath)
    {
        if (File.Exists(newSettingsPath))
            return;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] legacyPaths =
        {
            Path.Combine(localAppData, "D2RInfernalCompanion", "settings.json"),
            Path.Combine(localAppData, "D2RBuffTracker", "settings.json"),
            Path.Combine(localAppData, "D2RSaveSentinel", "settings.json")
        };

        foreach (string oldPath in legacyPaths)
        {
            try
            {
                if (!File.Exists(oldPath))
                    continue;

                File.Copy(oldPath, newSettingsPath, overwrite: false);
                return;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }

    public static void ApplyTheme(AppTheme theme)
    {
        var applied = theme switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.System => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark,
            _ => ApplicationTheme.Dark
        };
        ApplicationThemeManager.Apply(applied);
    }
}
