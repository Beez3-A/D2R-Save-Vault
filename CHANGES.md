# Changelog

## 1.0.2

- Fixed Light mode so the custom Save Vault interface now follows the selected application theme correctly.
- Updated the main window background, sidebar, cards, and card borders to use theme-aware colours.
- Theme changes now update the custom interface immediately without requiring an application restart.
- Preserved the existing Dark mode appearance while improving Light mode readability and consistency.

## 1.0.1

- Restored a proper application title bar with **Minimize**, **Maximize/Restore**, and **Close** buttons.
- Restored normal window dragging by making the title bar the draggable caption area.
- Enabled `ExtendsContentIntoTitleBar` to match the WPF UI `FluentWindow` + Mica configuration.
- Kept the existing close-to-tray and minimize-to-tray settings: the new window buttons still respect those options.

## 1.0.0

- Renamed the backup-only application to **D2R Save Vault**.
- Set author, company, copyright and About-page credits to **ArcBee**.
- Removed the Buff Tracker UI, overlay, profiles, buff models, sounds/assets, tracking/input services and related Vortice packages.
- Removed Cube/Q Toggle UI, service, keybind models and tray controls.
- Fixed the backup destination field so a selected custom folder is shown immediately and remains visible even when the save folder is temporarily unavailable.
- Changed **Bind backups to D2R** behaviour:
  - D2R starts -> scheduled backup timer starts.
  - D2R exits -> scheduled backup timer stops.
  - Save Vault remains running in the system tray.
- Added a hard D2R-running check before each scheduled backup to prevent a timer tick after the game has already closed.
- Kept manual Backup Now and restore support; restores remain blocked while D2R is running and still create a safety snapshot first.
- Preserved migration of useful settings from the previous Infernal Companion / Buff Tracker local-app-data locations.
- Updated Windows startup registry name, project metadata, build workflow and documentation for the new backup-only application.
