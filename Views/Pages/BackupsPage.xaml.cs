using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using D2RSaveVault.Services;
using D2RSaveVault.ViewModels;

namespace D2RSaveVault.Views;

public partial class BackupsPage : UserControl
{
    public BackupsPage()
    {
        InitializeComponent();
        Loaded += BackupsPage_OnLoaded;
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void BackupsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        IntervalValueBox.Text = Vm.Settings.BackupIntervalValue.ToString();
        Vm.Backup.RefreshBackupRootPath();
        Vm.Backup.RefreshAvailableBackups();
        ForceBackupFolderTextRefresh();
    }

    private void ToggleBackups_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        bool ok = Vm.ToggleAutomaticBackups();
        if (!ok)
            ShowBackupSettingsWarning();
    }

    private void AutoStart_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || Vm == null)
            return;

        bool ok = Vm.OnAutomaticBackupsSettingChanged();
        if (!ok && Vm.Settings.AutoStartBackups)
            ShowBackupSettingsWarning();
    }

    private void ShowBackupSettingsWarning()
    {
        MessageBox.Show(
            Window.GetWindow(this),
            "Please select a valid Diablo II: Resurrected save folder, backup folder, and backup interval first.",
            "Backup settings required",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void IntervalValue_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        if (!int.TryParse(IntervalValueBox.Text, out int value) || value < 1)
            value = 1;
        value = Math.Clamp(value, 1, 2_000_000);
        Vm.Settings.BackupIntervalValue = value;
        IntervalValueBox.Text = value.ToString();
        Vm.ApplyBackupIntervalSettings();
    }

    private void IntervalUnit_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Vm?.ApplyBackupIntervalSettings();
    }

    private void DecreaseRetention_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;
        Vm.Settings.MaxBackups = Math.Max(1, Vm.Settings.MaxBackups - 1);
        Vm.Settings.Save();
    }

    private void IncreaseRetention_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;
        Vm.Settings.MaxBackups = Math.Min(100, Vm.Settings.MaxBackups + 1);
        Vm.Settings.Save();
    }

    private void BrowseSaveFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select your Diablo II Resurrected save folder"
        };
        if (Directory.Exists(Vm.Settings.BackupSavePath))
            dialog.SelectedPath = Vm.Settings.BackupSavePath;

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        if (!File.Exists(Path.Combine(dialog.SelectedPath, "Settings.json")))
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Settings.json was not detected. This does not appear to be the Diablo II: Resurrected save folder.",
                "Incorrect save folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Vm.Settings.BackupSavePath = dialog.SelectedPath;
        Vm.OnBackupPathChanged();
        ForceBackupFolderTextRefresh();
    }

    private void BackupFolderBox_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;
        e.Handled = true;
        ChooseBackupFolder();
    }

    private void BrowseBackupFolder_OnClick(object sender, RoutedEventArgs e)
        => ChooseBackupFolder();

    private void ChooseBackupFolder()
    {
        if (Vm == null)
            return;

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Choose where D2R Save Vault should store backups"
        };

        string current = Vm.Backup.BackupRootPath;
        if (Directory.Exists(current))
            dialog.SelectedPath = current;

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        string chosen = Path.GetFullPath(dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string save = string.IsNullOrWhiteSpace(Vm.Settings.BackupSavePath)
            ? string.Empty
            : Path.GetFullPath(Vm.Settings.BackupSavePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (!string.IsNullOrWhiteSpace(save) && IsSameOrChildPath(chosen, save))
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Please choose a backup folder outside the Diablo II: Resurrected save folder. Storing backups inside the save folder can cause recursive backups.",
                "Choose another backup folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Vm.Settings.BackupFolderPath = chosen;
        Vm.OnBackupFolderChanged();

        // Explicitly refresh the nested binding as well as raising the service's
        // PropertyChanged event. This fixes the case where the selected target
        // path was saved correctly but the read-only field remained visually blank.
        ForceBackupFolderTextRefresh();

        if (!string.Equals(Vm.Backup.BackupRootPath, chosen, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "The selected backup folder could not be opened. Please choose a folder that D2R Save Vault can write to.",
                "Backup folder unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ForceBackupFolderTextRefresh()
    {
        BackupFolderBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
    }

    private static bool IsSameOrChildPath(string candidate, string parent)
    {
        string c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string p = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return c.StartsWith(p, StringComparison.OrdinalIgnoreCase);
    }

    private async void BackupNow_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        if (!Vm.Backup.HasValidSavePath)
        {
            MessageBox.Show(Window.GetWindow(this),
                "Please select a valid Diablo II: Resurrected save folder first.",
                "Save folder required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await Vm.BackupNowAsync();
    }

    private async void Restore_OnClick(object sender, RoutedEventArgs e)
    {
        if (Vm == null)
            return;

        if (RestoreCombo.SelectedItem is not string backupName || string.IsNullOrWhiteSpace(backupName))
        {
            MessageBox.Show(Window.GetWindow(this), "Choose a backup from the list first.",
                "Restore backup", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (D2RProcessDetector.IsRunning())
        {
            MessageBox.Show(Window.GetWindow(this), "Close Diablo II: Resurrected before restoring a full backup.",
                "D2R is running", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            Window.GetWindow(this),
            "Restore this FULL D2R folder backup?\n\nThis will replace the current contents of your Diablo II: Resurrected save folder. A safety snapshot of the current folder will be created first.",
            "Restore Full Backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        bool ok = await Vm.RestoreBackupAsync(backupName);
        if (!ok)
        {
            MessageBox.Show(Window.GetWindow(this),
                "The restore could not be completed. Check the Activity log for details.",
                "Restore failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
