using System.Runtime.InteropServices;

namespace Axit.Forms;

/// <summary>
/// The Win32 calls and messages more than one piece of the bundle makes, declared once (2026-10-24: each was declared
/// two to four times). A file uses them through <c>using static Axit.Forms.User32;</c>. A call only one file makes stays
/// declared in that file, next to its use.
/// </summary>
public static class User32
{
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int EM_SCROLLCARET = 0x00B7;
    public const int EM_GETLINECOUNT = 0x00BA;

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
}
