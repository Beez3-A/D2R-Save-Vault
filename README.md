<p align="center">
  <img src="Assets/AppIcon_256.png" width="160" alt="D2R Save Vault Logo">
</p>

<h1 align="center">D2R Save Vault</h1>

<p align="center">
  Automatic full-folder save backups for Diablo II: Resurrected
</p>

<p align="center">
  <strong>Windows x64 • Version 1.0.2 • Created by ArcBee</strong>
</p>

---

## About

**D2R Save Vault** is a lightweight Windows utility for creating full-folder backups of your **Diablo II: Resurrected** save directory.

It can run quietly in the system tray, wait for Diablo II: Resurrected to start, automatically create backups while the game is running, and stop scheduled backups when the game closes.

Everything runs locally on your PC.

## Features

- Full-folder snapshots of your Diablo II: Resurrected save directory
- Automatic backups at a configurable interval
- **Bind backups to D2R** mode
  - waits while D2R is closed
  - starts scheduled backups when D2R opens
  - stops scheduled backups when D2R exits
- Manual **Backup now** option
- Custom backup destination
- Configurable number of backups to retain
- Automatic cleanup of older backups after the retention limit is reached
- Restore previous backups from inside the app
- Safety snapshot created before a restore
- Restore blocked while Diablo II: Resurrected is running
- Launch with Windows
- Start minimized to the system tray
- Minimize to tray
- Keep running in the tray when the window is closed
- Dark, Light, and System themes
- Self-contained Windows x64 release

## Requirements

- Windows x64
- Diablo II: Resurrected
- No separate .NET runtime installation is required for the published self-contained release

## Installation

1. Open the **Releases** section of this repository.
2. Download the latest Windows archive, for example:

   ```text
   D2R-Save-Vault-v1.0.2-Windows-x64.zip
   ```

3. Extract the ZIP to a permanent folder, for example:

   ```text
   C:\Tools\D2R Save Vault
   ```

4. Run:

   ```text
   D2RSaveVault.exe
   ```

5. On first launch, choose your Diablo II: Resurrected save folder and your preferred backup folder.
6. Configure the backup interval and the maximum number of backups you want to keep.
7. Enable **Automatic backups**.
8. If you only want scheduled backups while Diablo II: Resurrected is running, enable **Bind backups to D2R**.

> **Windows SmartScreen:** D2R Save Vault is currently unsigned, so Windows may show a SmartScreen warning. Download releases only from the official GitHub repository and do not disable SmartScreen globally.

## First-time Setup

### 1. Select the D2R save folder

The usual Diablo II: Resurrected save location is:

```text
%USERPROFILE%\Saved Games\Diablo II Resurrected
```

Use **Browse...** in D2R Save Vault if your save folder is somewhere else.

### 2. Select a backup folder

Choose a folder where Save Vault should store its snapshots.

The backup destination must not be the D2R save folder itself or a folder inside it.

If you do not choose a custom destination, Save Vault creates:

```text
D2R Save Vault Backups
```

next to your configured D2R save folder.

### 3. Configure the schedule

Choose:

- the backup interval
- Seconds, Minutes, or Hours
- the maximum number of backups to keep

When the retention limit is reached, the oldest backups are removed automatically.

## Recommended Setup

For an automatic, low-maintenance setup, enable:

- **Automatic backups**
- **Bind backups to D2R**
- **Launch with Windows**
- **Launch minimized to system tray**
- **Minimize to system tray**
- **Keep running when closed**

With those options enabled:

```text
Windows starts
    ↓
D2R Save Vault starts quietly in the tray
    ↓
D2R is closed → Save Vault waits
    ↓
D2R starts → scheduled backups begin
    ↓
D2R exits → scheduled backups stop
    ↓
Save Vault remains in the tray, ready for the next session
```

## Manual Backups

You can create a backup at any time by clicking **Backup now**.

The tray menu also provides a **Backup now** command, so the main window does not need to remain open.

## Restoring a Backup

1. Close Diablo II: Resurrected.
2. Open D2R Save Vault.
3. Go to **Backups**.
4. Select the backup you want from the restore list.
5. Click **Restore**.
6. Confirm the restore.

Before replacing the current contents of your D2R save folder, Save Vault creates a **safety snapshot** of the current folder.

D2R Save Vault will not perform a restore while Diablo II: Resurrected is running.

> For characters or saves that are especially important to you, keeping an additional independent backup is still recommended.

## System Tray Behaviour

Depending on your Options settings:

- **Minimize** can hide Save Vault to the system tray.
- Clicking **X** can keep the app running in the tray instead of exiting.
- Double-click the tray icon to reopen the window.
- Right-click the tray icon for **Open**, **Backup now**, and **Exit**.
- Use **Exit** from the tray menu when you want to fully close the application.

## Settings and Logs

D2R Save Vault stores its local configuration in:

```text
%LOCALAPPDATA%\D2RSaveVault\settings.json
```

The application log is stored in:

```text
%LOCALAPPDATA%\D2RSaveVault\log.txt
```

These files are useful when troubleshooting or reporting a bug.

## Building From Source

D2R Save Vault is a WPF application targeting **.NET 10 for Windows**.

With the .NET 10 SDK installed:

```powershell
dotnet restore D2RSaveVault.csproj

dotnet publish D2RSaveVault.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output publish `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeSourceRevisionInInformationalVersion=false
```

A GitHub Actions build workflow is also included at:

```text
.github/workflows/build.yml
```

## Reporting Issues

If you find a bug, open an issue on GitHub and include:

- D2R Save Vault version
- Windows version
- what you expected to happen
- what actually happened
- steps to reproduce the problem
- a screenshot when useful
- relevant entries from the **Activity** panel or `log.txt`

Please avoid attaching personal save files unless they are specifically needed to reproduce an issue.

## Version

Current release: **v1.0.2**

See [`CHANGES.md`](CHANGES.md) for version history.

## Author

**ArcBee**

## Support the project

If D2R Save Vault has been useful to you and you'd like to support continued development, you can buy me a coffee via PayPal:

[Support D2R Save Vault on PayPal](https://paypal.me/arcbeematt?locale.x=en_US&country.x=ZA)

Support is completely optional. The project will remain free and open source.

## License

D2R Save Vault is released under the **GNU General Public License v3.0**.

See [`LICENSE`](LICENSE) for the full license text.

## Disclaimer

D2R Save Vault is an independent community project and is not affiliated with, endorsed by, or associated with Blizzard Entertainment.

Diablo II: Resurrected and related trademarks are property of Blizzard Entertainment, Inc.
