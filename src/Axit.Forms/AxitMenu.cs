using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Forms;

/// <summary>
/// The Axit menu every window has (docs/axit/SPEC.md AX-9), one class for every window: the window's Command palette
/// item (AX-10); New window (one item per app) and Recently closed (the bundle's list, AX-9.4); Open windows (the
/// other windows of the bundle's apps, most recently used first), Open windows by type (one submenu per app, the
/// current window checked) and Close all windows; the window's own Check for updates and Update to items, and About,
/// the bundle's sections (AX-9.9). The lists are filled
/// each time the menu opens, the windows from what the system lists in Z order, which is Alt+Tab's order, so nothing
/// about windows is stored. The lists and labels are <see cref="AxitWindows"/> and <see cref="RecentlyClosed"/>, the
/// About texts <see cref="BundleAbout"/>, tested; the rest is checked with NVDA.
/// </summary>
public sealed class AxitMenu : ToolStripMenuItem
{
    private readonly Form _owner;
    private readonly ToolStripMenuItem _recentlyClosed = new("&Recently closed");
    private readonly ToolStripMenuItem _openWindows = new("&Open windows");
    private readonly List<(App App, ToolStripMenuItem Item)> _perApp = [];

    /// <param name="owner">The window, whose own handle is the one checked in the lists and closed last by Close all windows.</param>
    /// <param name="commandPalette">The window's Command palette item (AX-10.1), the menu's first.</param>
    /// <param name="checkForUpdates">The window's Check for updates item (AX-3.1).</param>
    /// <param name="updateTo">The window's Update to item, visible while a newer release is known.</param>
    /// <param name="latestRelease">The window's latest-release state for About, Version.</param>
    /// <param name="showNotice">Shows an About section as a notice in the window (AX-7.2).</param>
    public AxitMenu(Form owner, ToolStripMenuItem commandPalette, ToolStripMenuItem checkForUpdates, ToolStripMenuItem updateTo, Func<string> latestRelease, Action<Notice> showNotice)
        : base("&Axit")
    {
        _owner = owner;
        var newWindow = new ToolStripMenuItem("&New window");
        var byType = new ToolStripMenuItem("Open windows &by type");
        foreach (var app in Dispatch.Apps)
        {
            var verb = app.Verb;
            newWindow.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.AppLabel(app), null, (_, _) => Start(verb, null)));
            var list = new ToolStripMenuItem(AxitWindows.AppLabel(app));
            byType.DropDownItems.Add(list);
            _perApp.Add((app, list));
        }

        var about = new ToolStripMenuItem("&About");
        about.DropDownItems.Add(new ToolStripMenuItem("&Version", null, (_, _) => showNotice(Notice.Plain(BundleAbout.VersionTitle, BundleAbout.Version(latestRelease())))));
        about.DropDownItems.Add(new ToolStripMenuItem("&Licence", null, (_, _) => showNotice(Notice.Plain(BundleAbout.LicenceTitle, BundleAbout.Licence))));
        about.DropDownItems.Add(new ToolStripMenuItem("&Attribution", null, (_, _) => showNotice(Notice.Plain(BundleAbout.AttributionTitle, BundleAbout.Attribution))));
        about.DropDownItems.Add(new ToolStripMenuItem("Axit &guide", null, (_, _) => showNotice(Notice.Plain(BundleAbout.GuideTitle, BundleAbout.Guide()))));
        about.DropDownItems.Add(new ToolStripMenuItem("Axit on Git&Hub", null, (_, _) => OpenUrl(UpdateCheck.RepositoryPage)));

        DropDownItems.Add(commandPalette);
        DropDownItems.Add(new ToolStripSeparator());
        DropDownItems.Add(newWindow);
        // The lists made as the menu opens; the command palette leaves them out (AX-10.2).
        DropDownItems.Add(CommandPalette.FilledOnOpening(_recentlyClosed));
        DropDownItems.Add(new ToolStripSeparator());
        DropDownItems.Add(CommandPalette.FilledOnOpening(_openWindows));
        DropDownItems.Add(CommandPalette.FilledOnOpening(byType));
        DropDownItems.Add(new ToolStripMenuItem("&Close all windows", null, (_, _) => CloseAll()));
        DropDownItems.Add(new ToolStripSeparator());
        DropDownItems.Add(checkForUpdates);
        DropDownItems.Add(updateTo);
        DropDownItems.Add(about);
        DropDownOpening += (_, _) => Fill();
        Fill();
    }

    private void Fill()
    {
        _recentlyClosed.DropDownItems.Clear();
        var closed = RecentlyClosed.Load();
        for (var i = 0; i < closed.Count; i++)
        {
            var item = closed[i];
            if (Dispatch.Apps.FirstOrDefault(a => a.Name == item.App) is { } app)
            {
                _recentlyClosed.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, RecentlyClosed.Title(item)), null, (_, _) => Start(app.Verb, item.Path)));
            }
        }

        NoneWhenEmpty(_recentlyClosed, "&Nothing recently closed");

        var windows = OpenWindows();
        var current = Current();
        FillWindows(_openWindows, AxitWindows.Others(windows, current), current, "&No other open windows");
        foreach (var (app, item) in _perApp)
        {
            var of = AxitWindows.Of(windows, app);
            item.Text = $"{AxitWindows.AppLabel(app)} ({of.Count})";
            FillWindows(item, of, current, $"&No open {app.Name} windows");
        }
    }

    private nint Current() => _owner.IsHandleCreated ? _owner.Handle : 0;

    private static void FillWindows(ToolStripMenuItem list, IReadOnlyList<AxitWindow> windows, nint current, string none)
    {
        list.DropDownItems.Clear();
        for (var i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            list.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, window.Title), null, (_, _) => Activate(window.Handle))
            {
                Checked = window.Handle == current,
            });
        }

        NoneWhenEmpty(list, none);
    }

    /// <summary>
    /// An empty list gets one item that says so and does nothing, enabled (AX-9.5): WinForms skips disabled items when
    /// the keyboard moves through a menu, so a submenu of only a disabled "(none)" opened with nothing selected and
    /// left the screen reader with nothing to read.
    /// </summary>
    private static void NoneWhenEmpty(ToolStripMenuItem list, string text)
    {
        if (list.DropDownItems.Count == 0)
        {
            list.DropDownItems.Add(new ToolStripMenuItem(text));
        }
    }

    /// <summary>
    /// AX-9.8: the close command Alt+F4 sends, posted to every other Axit window and then to this one; each window
    /// closes as it does on Alt+F4 and asks there what it would ask then (unsaved changes, Claude working).
    /// </summary>
    private void CloseAll()
    {
        var current = Current();
        foreach (var window in AxitWindows.Others(OpenWindows(), current))
        {
            PostMessage(window.Handle, WM_SYSCOMMAND, SC_CLOSE, 0);
        }

        if (current != 0)
        {
            PostMessage(current, WM_SYSCOMMAND, SC_CLOSE, 0);
        }
    }

    /// <summary>The visible, unowned, titled top-level windows of every process with this one's name, in Z order: the most recently used first.</summary>
    public static IReadOnlyList<AxitWindow> OpenWindows()
    {
        var processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
        var ids = new HashSet<int>(processes.Select(p => p.Id));
        foreach (var process in processes)
        {
            process.Dispose();
        }

        var windows = new List<AxitWindow>();
        EnumWindows((handle, _) =>
        {
            if (IsWindowVisible(handle) && GetWindow(handle, GW_OWNER) == 0)
            {
                GetWindowThreadProcessId(handle, out var id);
                if (ids.Contains((int)id))
                {
                    var title = TitleOf(handle);
                    if (title.Length > 0)
                    {
                        windows.Add(new AxitWindow(handle, title));
                    }
                }
            }

            return true;
        }, 0);
        return windows;
    }

    /// <summary>Brings the window to the front, restored when it was minimized. Windows allows it because the user's own key in the foreground window asked.</summary>
    public static void Activate(nint handle)
    {
        if (IsIconic(handle))
        {
            ShowWindow(handle, SW_RESTORE);
        }

        SetForegroundWindow(handle);
    }

    /// <summary>A new window of the app: this program file with the app's verb and, for a recently closed item, its path; a new process (B-2).</summary>
    public static void Start(string verb, string? path) => StartWith(verb, path is null ? [] : [path]);

    /// <summary>The app started with several arguments after its verb (AxMedia's paths and --add, a viewer's --new); a new process, which may hand its paths to the app's open window and end (AppMessage).</summary>
    public static void StartWith(string verb, IReadOnlyList<string> arguments)
    {
        if (Environment.ProcessPath is not { } exe)
        {
            return;
        }

        var info = new ProcessStartInfo(exe) { UseShellExecute = false };
        info.ArgumentList.Add(verb);
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        Process.Start(info);
    }

    /// <summary>The page in the default browser, one of the system's own windows; a failure is logged, as nothing can be shown while the menu closes.</summary>
    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Error($"The page {url} could not be opened", ex);
        }
    }

    private static string TitleOf(nint handle)
    {
        var length = GetWindowTextLength(handle);
        if (length == 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        GetWindowText(handle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private const uint GW_OWNER = 4;
    private const int SW_RESTORE = 9;
    private const uint WM_SYSCOMMAND = 0x0112;
    private const nint SC_CLOSE = 0xF060;

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint cmd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder text, int max);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int cmd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
