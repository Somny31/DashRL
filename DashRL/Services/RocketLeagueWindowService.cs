using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DashRL.Services;

public static class RocketLeagueWindowService
{
    // ================================================================
    // STRUCTURES WINDOWS
    // ================================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public uint cbSize;

        public RECT rcMonitor;

        public RECT rcWork;

        public uint dwFlags;
    }

    // ================================================================
    // WINDOWS API
    // ================================================================

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId
    );

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        uint dwFlags
    );

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Auto
    )]
    private static extern bool GetMonitorInfo(
        IntPtr hMonitor,
        ref MONITORINFO lpmi
    );

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    // ================================================================
    // FENÊTRE ROCKET LEAGUE
    // ================================================================

    public static IntPtr GetRocketLeagueWindow()
    {
        try
        {
            Process[] processes =
                Process.GetProcessesByName(
                    "RocketLeague"
                );

            foreach (Process process in processes)
            {
                try
                {
                    if (process.MainWindowHandle !=
                        IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch
        {
            // Rocket League n'est probablement pas lancé.
        }

        return IntPtr.Zero;
    }

    // ================================================================
    // FENÊTRE ACTUELLEMENT ACTIVE
    // ================================================================

    private static string? GetForegroundProcessName()
    {
        try
        {
            IntPtr foregroundWindow =
                GetForegroundWindow();

            if (foregroundWindow == IntPtr.Zero)
                return null;

            GetWindowThreadProcessId(
                foregroundWindow,
                out uint processId
            );

            if (processId == 0)
                return null;

            using Process process =
                Process.GetProcessById(
                    (int)processId
                );

            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    // ================================================================
    // ROCKET LEAGUE AU PREMIER PLAN
    // ================================================================

    public static bool IsRocketLeagueForeground()
    {
        string? processName =
            GetForegroundProcessName();

        if (string.IsNullOrWhiteSpace(
            processName))
        {
            return false;
        }

        return string.Equals(
            processName,
            "RocketLeague",
            StringComparison.OrdinalIgnoreCase
        );
    }

    // ================================================================
    // DASHRL AU PREMIER PLAN
    // ================================================================

    public static bool IsDashRLForeground()
    {
        string? processName =
            GetForegroundProcessName();

        if (string.IsNullOrWhiteSpace(
            processName))
        {
            return false;
        }

        string currentProcessName =
            Process
                .GetCurrentProcess()
                .ProcessName;

        return string.Equals(
            processName,
            currentProcessName,
            StringComparison.OrdinalIgnoreCase
        );
    }

    // ================================================================
    // OVERLAY AUTORISÉ
    // ================================================================

    public static bool ShouldShowOverlay()
    {
        return
            IsRocketLeagueForeground() ||
            IsDashRLForeground();
    }

    // ================================================================
    // ÉCRAN UTILISÉ PAR ROCKET LEAGUE
    // ================================================================

    public static Rect? GetRocketLeagueMonitorBounds()
    {
        try
        {
            IntPtr rocketLeagueWindow =
                GetRocketLeagueWindow();

            if (rocketLeagueWindow ==
                IntPtr.Zero)
            {
                return null;
            }

            IntPtr monitor =
                MonitorFromWindow(
                    rocketLeagueWindow,
                    MONITOR_DEFAULTTONEAREST
                );

            if (monitor == IntPtr.Zero)
                return null;

            MONITORINFO monitorInfo =
                new()
                {
                    cbSize =
                        (uint)Marshal.SizeOf<MONITORINFO>()
                };

            bool success =
                GetMonitorInfo(
                    monitor,
                    ref monitorInfo
                );

            if (!success)
                return null;

            double x =
                monitorInfo.rcMonitor.Left;

            double y =
                monitorInfo.rcMonitor.Top;

            double width =
                monitorInfo.rcMonitor.Right -
                monitorInfo.rcMonitor.Left;

            double height =
                monitorInfo.rcMonitor.Bottom -
                monitorInfo.rcMonitor.Top;

            return new Rect(
                x,
                y,
                width,
                height
            );
        }
        catch
        {
            return null;
        }
    }
}