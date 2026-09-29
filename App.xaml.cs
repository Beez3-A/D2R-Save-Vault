using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
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
        ApplyCustomThemeResources(applied);
    }

    private static void ApplyCustomThemeResources(ApplicationTheme theme)
    {
        if (Current?.Resources == null)
            return;

        bool light = theme == ApplicationTheme.Light;

        Current.Resources["InfernalWindowBackgroundBrush"] = light
            ? CreateGradient(0xF7, 0xF7, 0xF7, 0xF1, 0xF1, 0xF1, 0xF7, 0xF7, 0xF7, horizontal: true)
            : CreateGradient(0x30, 0x30, 0x30, 0x34, 0x34, 0x34, 0x30, 0x30, 0x30, horizontal: true);

        Current.Resources["InfernalSidebarBrush"] = light
            ? CreateGradient(0xF1, 0xF1, 0xF1, 0xEC, 0xEC, 0xEC, 0xEC, 0xEC, 0xEC, horizontal: false)
            : CreateGradient(0x3D, 0x3D, 0x3D, 0x39, 0x39, 0x39, 0x39, 0x39, 0x39, horizontal: false);

        Current.Resources["InfernalCardBrush"] = light
            ? CreateGradient(0xFF, 0xFF, 0xFF, 0xF8, 0xF8, 0xF8, 0xF8, 0xF8, 0xF8, horizontal: true)
            : CreateGradient(0x41, 0x41, 0x41, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, horizontal: true);

        Current.Resources["InfernalCardBorderBrush"] = new SolidColorBrush(
            light ? Color.FromRgb(0xD8, 0xD8, 0xD8) : Color.FromRgb(0x50, 0x50, 0x50));
    }

    private static LinearGradientBrush CreateGradient(
        byte r1, byte g1, byte b1,
        byte r2, byte g2, byte b2,
        byte r3, byte g3, byte b3,
        bool horizontal)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = horizontal ? new Point(1, 1) : new Point(0, 1)
        };

        brush.GradientStops.Add(new GradientStop(Color.FromRgb(r1, g1, b1), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(r2, g2, b2), 0.52));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(r3, g3, b3), 1));
        return brush;
    }
}
