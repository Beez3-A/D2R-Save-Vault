using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace D2RSaveVault.Services;

/// <summary>
/// Centralised Diablo II: Resurrected process/window detection.
///
/// Process-name detection is the primary path. Window detection is used as a
/// fallback because Windows can occasionally deny or return incomplete process
/// information across privilege boundaries (for example when D2R is elevated).
/// Keeping this logic in one place ensures automatic backup lifecycle and restore
/// safety checks agree about whether the game is running.
/// </summary>
public static class D2RProcessDetector
{
    private const string GameProcessName = "D2R";
    private const string GameWindowTitle = "D2R";
    private const string GameWindowTitleLong = "Diablo II: Resurrected";

    /// <summary>Returns true while a D2R process or verified D2R window exists.</summary>
    public static bool IsRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName(GameProcessName);
            try
            {
                if (processes.Length > 0)
                    return true;
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }
        }
        catch
        {
            // Fall through to top-level-window detection below.
        }

        return HasGameWindow();
    }

    /// <summary>Returns true only when the current foreground window belongs to D2R.</summary>
    public static bool IsForeground()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        return IsGameWindow(hwnd);
    }

    /// <summary>
    /// Determines whether a top-level window belongs to D2R. If the owning
    /// process can be inspected, its name must be D2R. The title fallback is
    /// intentionally used only when process inspection fails, preventing a
    /// browser tab containing "Diablo II: Resurrected" from being mistaken for
    /// the game.
    /// </summary>
    public static bool IsGameWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0)
            return false;

        bool processInspected = false;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            string processName = process.ProcessName;
            processInspected = true;
            if (IsGameProcessName(processName))
                return true;
        }
        catch
        {
            // UIPI/protected-process/race failures are handled by title fallback.
        }

        if (processInspected)
            return false;

        return IsGameWindowTitle(GetWindowTitle(hwnd));
    }

    internal static bool IsGameProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        string normalized = Path.GetFileNameWithoutExtension(processName.Trim());
        return string.Equals(normalized, GameProcessName, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGameWindowTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        string normalized = title.Trim();
        return string.Equals(normalized, GameWindowTitle, StringComparison.OrdinalIgnoreCase)
               || normalized.Contains(GameWindowTitleLong, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasGameWindow()
    {
        bool found = false;
        try
        {
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hwnd))
                    return true;

                if (!IsGameWindow(hwnd))
                    return true;

                found = true;
                return false;
            }, IntPtr.Zero);
        }
        catch
        {
            return false;
        }

        return found;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int length = NativeMethods.GetWindowTextLength(hwnd);
        if (length <= 0)
            return string.Empty;

        var text = new StringBuilder(length + 1);
        return NativeMethods.GetWindowText(hwnd, text, text.Capacity) > 0
            ? text.ToString()
            : string.Empty;
    }

    private static class NativeMethods
    {
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern int GetWindowTextLength(IntPtr hWnd);
    }
}
