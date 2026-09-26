using System.Windows.Media;

namespace D2RSaveVault.Models;

public sealed class BackupLogEntry
{
    public required string Text { get; init; }
    public Brush Foreground { get; init; } = Brushes.LightGray;
}
