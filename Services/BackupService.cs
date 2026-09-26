using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using D2RSaveVault.Models;
using D2RSaveVault.Mvvm;

namespace D2RSaveVault.Services;

/// <summary>
/// Full-folder save backup and restore engine for Diablo II: Resurrected.
/// Scheduled backups can be hard-bound to the D2R process so the timer only
/// runs while the game is actually open.
/// </summary>
public sealed class BackupService : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private bool _isAutomaticBackupsRunning;
    private bool _isBusy;
    private string _statusText = "Paused — automatic backups disabled";
    private string _backupRootPath = string.Empty;

    public ObservableCollection<string> AvailableBackups { get; } = new();
    public ObservableCollection<BackupLogEntry> Activity { get; } = new();

    public bool IsAutomaticBackupsRunning
    {
        get => _isAutomaticBackupsRunning;
        private set => SetProperty(ref _isAutomaticBackupsRunning, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string BackupRootPath
    {
        get => _backupRootPath;
        private set => SetProperty(ref _backupRootPath, value);
    }

    public BackupService(AppSettings settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += OnAutomaticBackupTimerTick;

        if (string.IsNullOrWhiteSpace(_settings.BackupSavePath))
        {
            string defaultSavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Saved Games",
                "Diablo II Resurrected");
            if (Directory.Exists(defaultSavePath))
            {
                _settings.BackupSavePath = defaultSavePath;
                _settings.Save();
            }
        }

        RefreshBackupRootPath();
        RefreshAvailableBackups();
    }

    private async void OnAutomaticBackupTimerTick(object? sender, EventArgs e)
    {
        // Hard guard: when bound to D2R, never create a scheduled backup after
        // the game has exited, even if the one-second lifecycle watcher has not
        // observed the exit yet.
        if (_settings.BindToD2R && !D2RProcessDetector.IsRunning())
        {
            StopAutomaticTimer("Waiting for D2R — backups start when the game opens");
            return;
        }

        await PerformBackupAsync();
    }

    public void InitializeAutomaticBackups(bool d2rRunning, bool bindToD2R)
    {
        bool ok = SyncAutomaticBackups(d2rRunning, bindToD2R, logChange: false);
        if (!_settings.AutoStartBackups)
        {
            AppendLog("Automatic backups are disabled.");
        }
        else if (bindToD2R && !d2rRunning)
        {
            AppendLog("Automatic backups armed. Waiting for D2R to start.");
        }
        else if (ok && IsAutomaticBackupsRunning)
        {
            AppendLog(bindToD2R
                ? "D2R detected. Automatic backups started."
                : "Automatic backups started.", success: true);
        }
    }

    /// <summary>
    /// Reconciles the actual timer with the user's settings and D2R state.
    /// Waiting for D2R is considered a valid state, not an error.
    /// </summary>
    public bool SyncAutomaticBackups(bool d2rRunning, bool bindToD2R, bool logChange)
    {
        if (!_settings.AutoStartBackups)
        {
            StopAutomaticTimer("Paused — automatic backups disabled");
            if (logChange)
                AppendLog("Automatic backups disabled.");
            return true;
        }

        if (bindToD2R && !d2rRunning)
        {
            StopAutomaticTimer("Waiting for D2R — backups start when the game opens");
            if (logChange)
                AppendLog("Automatic backups armed. Waiting for D2R to start.");
            return true;
        }

        if (!Directory.Exists(_settings.BackupSavePath))
        {
            StopAutomaticTimer("Paused — select a valid D2R save folder");
            if (logChange)
                AppendLog("Automatic backups could not start because the D2R save folder is not available.", error: true);
            return false;
        }

        if (!TryGetInterval(out var interval))
        {
            StopAutomaticTimer("Paused — choose a valid backup interval");
            if (logChange)
                AppendLog("Automatic backups could not start because the interval is invalid.", error: true);
            return false;
        }

        RefreshBackupRootPath();
        if (string.IsNullOrWhiteSpace(BackupRootPath))
        {
            StopAutomaticTimer("Paused — choose a valid backup folder");
            if (logChange)
                AppendLog("Automatic backups could not start because the backup folder is invalid or inaccessible.", error: true);
            return false;
        }

        _timer.Stop();
        _timer.Interval = interval;
        _timer.Start();
        IsAutomaticBackupsRunning = true;
        StatusText = bindToD2R
            ? "Running — D2R is open"
            : "Running — automatic backups enabled";

        if (logChange)
            AppendLog("Automatic full-folder backups enabled.", success: true);
        return true;
    }

    public void HandleD2RStateChanged(bool isRunning, bool bindToD2R)
    {
        if (!bindToD2R)
        {
            SyncAutomaticBackups(isRunning, bindToD2R, logChange: false);
            return;
        }

        if (!_settings.AutoStartBackups)
        {
            StopAutomaticTimer("Paused — automatic backups disabled");
            return;
        }

        if (isRunning)
        {
            bool ok = SyncAutomaticBackups(true, true, logChange: false);
            if (ok && IsAutomaticBackupsRunning)
                AppendLog("D2R started. Automatic backups started.", success: true);
            else
                AppendLog("D2R started, but automatic backups could not start. Check the save folder, backup folder, and interval.", error: true);
            return;
        }

        bool wasRunning = IsAutomaticBackupsRunning;
        StopAutomaticTimer("Waiting for D2R — backups start when the game opens");
        if (wasRunning)
            AppendLog("D2R exited. Automatic backups stopped.");
    }

    public void ApplyIntervalSettings(bool d2rRunning, bool bindToD2R)
        => SyncAutomaticBackups(d2rRunning, bindToD2R, logChange: false);

    public void OnSavePathChanged(bool d2rRunning, bool bindToD2R)
    {
        RefreshBackupRootPath();
        RefreshAvailableBackups();
        SyncAutomaticBackups(d2rRunning, bindToD2R, logChange: false);
    }

    public void OnBackupFolderChanged(bool d2rRunning, bool bindToD2R)
    {
        RefreshBackupRootPath();
        RefreshAvailableBackups();
        SyncAutomaticBackups(d2rRunning, bindToD2R, logChange: false);
    }

    private void StopAutomaticTimer(string status)
    {
        _timer.Stop();
        IsAutomaticBackupsRunning = false;
        StatusText = status;
    }

    public void RefreshBackupRootPath()
    {
        try
        {
            BackupRootPath = GetBackupRootPath();
            if (!string.IsNullOrWhiteSpace(BackupRootPath))
                Directory.CreateDirectory(BackupRootPath);
        }
        catch (Exception ex)
        {
            BackupRootPath = string.Empty;
            Logger.Log(ex);
        }
    }

    public void RefreshAvailableBackups()
    {
        AvailableBackups.Clear();
        try
        {
            string root = GetBackupRootPath();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return;

            foreach (DirectoryInfo backup in new DirectoryInfo(root)
                         .GetDirectories("D2R_FullBackup_*")
                         .OrderByDescending(d => d.Name))
            {
                AvailableBackups.Add(backup.Name);
            }
        }
        catch (Exception ex)
        {
            AppendLog("Unable to read the backup folder: " + ex.Message, error: true);
        }
    }

    public async Task<bool> PerformBackupAsync()
    {
        if (!Directory.Exists(_settings.BackupSavePath))
        {
            AppendLog("Backup skipped — select a valid D2R save folder first.", error: true);
            return false;
        }

        if (!await _operationGate.WaitAsync(0))
            return false;

        IsBusy = true;
        try
        {
            var result = await Task.Run(CreateFullFolderBackup);
            await Task.Run(DeleteOldBackups);
            RefreshAvailableBackups();

            double sizeMb = result.TotalBytes / 1024d / 1024d;
            AppendLog($"Full folder backup completed — {result.FileCount} files, {sizeMb:F2} MB. Keeping newest {GetMaxBackups()}.", success: true);
            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"Backup failed — {ex.Message}", error: true);
            Logger.Log(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    public async Task<bool> RestoreAsync(string backupName)
    {
        if (string.IsNullOrWhiteSpace(backupName))
            return false;
        if (D2RProcessDetector.IsRunning())
            return false;

        string backupFolder;
        try { backupFolder = Path.Combine(GetBackupRootPath(), backupName); }
        catch { return false; }

        string saveFolder = _settings.BackupSavePath;
        if (!Directory.Exists(backupFolder) || !Directory.Exists(saveFolder))
            return false;

        if (!await _operationGate.WaitAsync(0))
            return false;

        IsBusy = true;
        try
        {
            await Task.Run(() =>
            {
                // Safety snapshot before the destructive restore.
                CreateFullFolderBackup();
                RestoreFolderContents(backupFolder, saveFolder);
                DeleteOldBackups();
            });

            AppendLog($"Restored full backup {backupName}", success: true);
            RefreshAvailableBackups();
            return true;
        }
        catch (Exception ex)
        {
            AppendLog("Restore failed — " + ex.Message, error: true);
            Logger.Log(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    public bool HasValidSavePath => Directory.Exists(_settings.BackupSavePath);

    private bool TryGetInterval(out TimeSpan interval)
    {
        interval = TimeSpan.Zero;
        int value = _settings.BackupIntervalValue;
        if (value <= 0)
            return false;

        try
        {
            interval = _settings.BackupIntervalUnit switch
            {
                BackupIntervalUnit.Seconds => TimeSpan.FromSeconds(value),
                BackupIntervalUnit.Minutes => TimeSpan.FromMinutes(value),
                BackupIntervalUnit.Hours => TimeSpan.FromHours(value),
                _ => TimeSpan.Zero
            };
            return interval > TimeSpan.Zero && interval.TotalMilliseconds <= int.MaxValue;
        }
        catch
        {
            return false;
        }
    }

    private string GetBackupRootPath()
    {
        string savePath = _settings.BackupSavePath;

        // A custom destination is a valid display target on its own. Do not hide
        // it just because the save folder is temporarily empty or unavailable.
        if (!string.IsNullOrWhiteSpace(_settings.BackupFolderPath))
        {
            string customRoot = Path.GetFullPath(_settings.BackupFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(savePath))
            {
                string fullSaveForValidation = Path.GetFullPath(savePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (IsSameOrChildPath(customRoot, fullSaveForValidation))
                    throw new InvalidOperationException("The backup folder cannot be the D2R save folder or a folder inside it.");
            }
            return customRoot;
        }

        if (string.IsNullOrWhiteSpace(savePath))
            return string.Empty;

        string fullSavePath = Path.GetFullPath(savePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        DirectoryInfo? parent = Directory.GetParent(fullSavePath);
        if (parent == null)
            throw new InvalidOperationException("Unable to determine the parent folder for the D2R save directory.");

        return Path.Combine(parent.FullName, "D2R Save Vault Backups");
    }

    private static bool IsSameOrChildPath(string candidate, string parent)
    {
        string normalizedCandidate = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string normalizedParent = Path.GetFullPath(parent)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }

    private int GetMaxBackups() => Math.Clamp(_settings.MaxBackups, 1, 100);

    private (string BackupFolder, int FileCount, long TotalBytes) CreateFullFolderBackup()
    {
        string sourceRoot = Path.GetFullPath(_settings.BackupSavePath);
        string backupRoot = GetBackupRootPath();

        if (!Directory.Exists(sourceRoot))
            throw new DirectoryNotFoundException("The configured D2R save folder does not exist: " + sourceRoot);

        Directory.CreateDirectory(backupRoot);
        string backupFolder = Path.Combine(backupRoot,
            "D2R_FullBackup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
        Directory.CreateDirectory(backupFolder);

        int fileCount = 0;
        long totalBytes = 0;

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relativeDirectory = Path.GetRelativePath(sourceRoot, directory);
                Directory.CreateDirectory(Path.Combine(backupFolder, relativeDirectory));
            }

            foreach (string sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relativeFile = Path.GetRelativePath(sourceRoot, sourceFile);
                string destinationFile = Path.Combine(backupFolder, relativeFile);
                CopyFileWithRetry(sourceFile, destinationFile);
                fileCount++;
                totalBytes += new FileInfo(sourceFile).Length;
            }

            return (backupFolder, fileCount, totalBytes);
        }
        catch
        {
            try
            {
                if (Directory.Exists(backupFolder))
                    Directory.Delete(backupFolder, true);
            }
            catch { }
            throw;
        }
    }

    private static void CopyFileWithRetry(string sourceFile, string destinationFile)
    {
        const int maxAttempts = 5;
        Exception? lastException = null;
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                FileInfo before = new(sourceFile);
                long lengthBefore = before.Length;
                DateTime writeBefore = before.LastWriteTimeUtc;

                File.Copy(sourceFile, destinationFile, true);

                FileInfo after = new(sourceFile);
                if (after.Length != lengthBefore || after.LastWriteTimeUtc != writeBefore)
                {
                    if (File.Exists(destinationFile))
                        File.Delete(destinationFile);
                    Thread.Sleep(150 * attempt);
                    continue;
                }

                File.SetLastWriteTimeUtc(destinationFile, writeBefore);
                return;
            }
            catch (IOException ex) { lastException = ex; }
            catch (UnauthorizedAccessException ex) { lastException = ex; }

            Thread.Sleep(150 * attempt);
        }

        throw new IOException($"Could not copy '{sourceFile}' after {maxAttempts} attempts.", lastException);
    }

    private void DeleteOldBackups()
    {
        string root = GetBackupRootPath();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return;

        DirectoryInfo[] backups = new DirectoryInfo(root)
            .GetDirectories("D2R_FullBackup_*")
            .OrderByDescending(d => d.Name)
            .ToArray();

        foreach (DirectoryInfo oldBackup in backups.Skip(GetMaxBackups()))
        {
            try { oldBackup.Delete(true); }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                    AppendLog($"Cleanup warning: could not delete {oldBackup.Name}: {ex.Message}", error: true));
            }
        }
    }

    private static void RestoreFolderContents(string backupFolder, string saveFolder)
    {
        foreach (string file in Directory.GetFiles(saveFolder, "*", SearchOption.TopDirectoryOnly))
            File.Delete(file);
        foreach (string directory in Directory.GetDirectories(saveFolder, "*", SearchOption.TopDirectoryOnly))
            Directory.Delete(directory, true);

        foreach (string directory in Directory.EnumerateDirectories(backupFolder, "*", SearchOption.AllDirectories))
        {
            string relativeDirectory = Path.GetRelativePath(backupFolder, directory);
            Directory.CreateDirectory(Path.Combine(saveFolder, relativeDirectory));
        }

        foreach (string sourceFile in Directory.EnumerateFiles(backupFolder, "*", SearchOption.AllDirectories))
        {
            string relativeFile = Path.GetRelativePath(backupFolder, sourceFile);
            CopyFileWithRetry(sourceFile, Path.Combine(saveFolder, relativeFile));
        }
    }

    public void AppendLog(string message, bool success = false, bool error = false)
    {
        var brush = success
            ? System.Windows.Media.Brushes.LightGreen
            : error ? System.Windows.Media.Brushes.IndianRed : System.Windows.Media.Brushes.LightGray;
        Activity.Add(new BackupLogEntry
        {
            Text = $"{DateTime.Now:HH:mm}: {message}",
            Foreground = brush
        });

        while (Activity.Count > 200)
            Activity.RemoveAt(0);
    }

    public void Dispose()
    {
        _timer.Stop();
        // Do not dispose the semaphore here: a file copy may still be unwinding
        // on a worker thread and will release it in its finally block.
    }
}
