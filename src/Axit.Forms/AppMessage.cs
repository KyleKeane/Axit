using System.Runtime.InteropServices;
using System.Text;
using Axit.Core;

namespace Axit.Forms;

/// <summary>What one process hands another's window (docs/axmedia/SPEC.md AM-1.3): a verb and the paths it is about.</summary>
public sealed record HandOver(string Verb, IReadOnlyList<string> Paths);

/// <summary>
/// The hand-over between a starting process and the open window of the same app (docs/axmedia/SPEC.md AM-1.2,
/// AM-1.3, AM-D4; docs/axpreview/SPEC.md AP-1.2): an app that keeps one window (AxMedia, AxPreview) is started with
/// paths, finds its newest open window through the same window list the Axit menu reads, hands the paths over with
/// one <c>WM_COPYDATA</c> message carrying a text (the verb, then one path per line), and ends. For the verbs that
/// take the focus it first lets the window's process bring itself to the front (<c>AllowSetForegroundWindow</c>,
/// which this process may grant because the user's own command started it), so that the window activates itself
/// once the paths are applied and NVDA reads the new content, not the old. The window reads the message in its
/// <c>WndProc</c> through <see cref="Receive"/>. Win32 of the plainest kind: synchronous, one-way, nothing on the
/// network (G6). The text form is pure and tested in <c>tests/Axit.Tests</c>; the sending and receiving are checked
/// with the apps.
/// </summary>
public static class AppMessage
{
    /// <summary>AxMedia: the queue replaced by the paths (a file with its folder's media files after it, a folder's media files) and played.</summary>
    public const string Play = "play";

    /// <summary>AxMedia: the paths appended to the queue; nothing starts, and the focus stays where it was.</summary>
    public const string Add = "add";

    /// <summary>AxPreview: the one file shown.</summary>
    public const string Open = "open";

    /// <summary>AxStart: the hidden window shown with the focus in its field (docs/axstart/SPEC.md AS-1.2); no paths.</summary>
    public const string Show = "show";

    private const int WM_COPYDATA = 0x004A;
    private const uint SMTO_NORMAL = 0x0000;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint TimeoutMs = 5000;

    /// <summary>Marks a message as the bundle's, so that another program's WM_COPYDATA is left alone: the letters AXIT.</summary>
    private static readonly nint Signature = 0x41584954;

    /// <summary>Whether a verb brings the window to the front (the window does it itself, once the paths are applied): everything but an add, which leaves the user where they are (AM-D3).</summary>
    public static bool TakesFocus(string verb) => !string.Equals(verb, Add, StringComparison.Ordinal);

    /// <summary>The text of a message: the verb on the first line, then one path per line.</summary>
    public static string Format(string verb, IEnumerable<string> paths) => string.Join("\n", [verb, .. paths]);

    /// <summary>The verb and the paths back from a message's text; null when the text has no verb.</summary>
    public static HandOver? Parse(string text)
    {
        var lines = text.Split('\n').Select(line => line.TrimEnd('\r', '\0')).ToList();
        if (lines.Count == 0 || lines[0].Trim().Length == 0)
        {
            return null;
        }

        return new HandOver(lines[0].Trim(), lines.Skip(1).Where(line => line.Length > 0).ToList());
    }

    /// <summary>
    /// Hands the verb and the paths to the newest open window of the app (the first in Z order, as the Axit menu
    /// lists them); true when a window took them, so that the starting process can end without a window of its own.
    /// False when the app has no window, or the window did not answer within five seconds.
    /// </summary>
    public static bool Send(string appName, string verb, IReadOnlyList<string> paths)
    {
        if (Dispatch.Apps.FirstOrDefault(app => app.Name == appName) is not { } app)
        {
            return false;
        }

        var windows = AxitWindows.Of(AxitMenu.OpenWindows(), app);
        if (windows.Count == 0)
        {
            return false;
        }

        return SendTo(windows[0].Handle, appName, verb, paths);
    }

    /// <summary>
    /// Hands the verb and the paths to one window found by the caller: AxStart's, which is hidden and so not in the
    /// Axit menu's list (docs/axstart/SPEC.md AS-1.2). True when the window took them.
    /// </summary>
    public static bool SendTo(nint target, string appName, string verb, IReadOnlyList<string> paths)
    {
        if (TakesFocus(verb))
        {
            // The window brings itself to the front once it has applied the paths; this process, started by the
            // user's own command, passes its right to do so on to the window's process.
            GetWindowThreadProcessId(target, out var processId);
            AllowSetForegroundWindow(processId);
        }

        var bytes = Encoding.Unicode.GetBytes(Format(verb, paths) + "\0");
        var buffer = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            var data = new CopyData { Signature = Signature, Length = (uint)bytes.Length, Data = buffer };
            if (SendMessageTimeoutW(target, WM_COPYDATA, 0, ref data, SMTO_NORMAL | SMTO_ABORTIFHUNG, TimeoutMs, out var result) == 0 || result == 0)
            {
                Log.Error($"The open {appName} window did not take the {verb} message");
                return false;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        Log.Info($"Handed {verb} with {paths.Count} path(s) to the open {appName} window");
        return true;
    }

    /// <summary>
    /// For a window's <c>WndProc</c>: the verb and the paths of a hand-over message, or null for any other message
    /// (another program's WM_COPYDATA included). The window sets <c>m.Result</c> to 1 when it takes them, which is
    /// what <see cref="Send"/> waits for.
    /// </summary>
    public static HandOver? Receive(ref Message m)
    {
        if (m.Msg != WM_COPYDATA || m.LParam == 0)
        {
            return null;
        }

        var data = Marshal.PtrToStructure<CopyData>(m.LParam);
        if (data.Signature != Signature || data.Data == 0 || data.Length < 2)
        {
            return null;
        }

        return Parse(Marshal.PtrToStringUni(data.Data, (int)(data.Length / 2)) ?? string.Empty);
    }

    /// <summary>COPYDATASTRUCT: a number of the sender's choosing, the byte count, and the bytes.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct CopyData
    {
        public nint Signature;
        public uint Length;
        public nint Data;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageTimeoutW(nint hWnd, uint msg, nint wParam, ref CopyData lParam, uint flags, uint timeout, out nint result);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
