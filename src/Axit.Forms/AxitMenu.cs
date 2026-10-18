using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Forms;

/// <summary>
/// The Axit menu every window has (docs/axit/SPEC.md AX-9), one class for every window: the window's own update and
/// About items at the top with Axit on GitHub, then New window (one item per app) and Recently closed (the bundle's list, AX-9.4), then
/// Open windows (the other windows of the bundle's apps, most recently used first) and one submenu per app with its
/// windows, the current one checked. The lists are filled each time the menu opens, the windows from what the system
/// lists in Z order, which is Alt+Tab's order, so nothing about windows is stored. The lists and labels are
/// <see cref="AxitWindows"/> and <see cref="RecentlyClosed"/>, tested; the rest is checked with NVDA.
/// </summary>
public sealed class AxitMenu : ToolStripMenuItem
{
    private readonly Form _owner;
    private readonly ToolStripMenuItem _recentlyClosed = new("&Recently closed");
    private readonly ToolStripMenuItem _openWindows = new("&Open windows");
    private readonly List<(App App, ToolStripMenuItem Item)> _perApp = [];

    /// <param name="owner">The window, whose own handle is the one checked in the lists.</param>
    /// <param name="appItems">The window's own items for the top: Check for updates, Update to, About (AX-3.1).</param>
    public AxitMenu(Form owner, IReadOnlyList<ToolStripItem> appItems)
        : base("&Axit")
    {
        _owner = owner;
        foreach (var item in appItems)
        {
            DropDownItems.Add(item);
        }

        DropDownItems.Add(new ToolStripMenuItem("Axit on &GitHub", null, (_, _) => OpenUrl(UpdateCheck.RepositoryPage)));
        DropDownItems.Add(new ToolStripSeparator());
        var newWindow = new ToolStripMenuItem("&New window");
        foreach (var app in Dispatch.Apps)
        {
            var verb = app.Verb;
            newWindow.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.AppLabel(app), null, (_, _) => Start(verb, null)));
            _perApp.Add((app, new ToolStripMenuItem(AxitWindows.AppLabel(app))));
        }

        DropDownItems.Add(newWindow);
        DropDownItems.Add(_recentlyClosed);
        DropDownItems.Add(new ToolStripSeparator());
        DropDownItems.Add(_openWindows);
        foreach (var (_, item) in _perApp)
        {
            DropDownItems.Add(item);
        }

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

        NoneWhenEmpty(_recentlyClosed);

        var windows = OpenWindows();
        var current = _owner.IsHandleCreated ? _owner.Handle : 0;
        FillWindows(_openWindows, AxitWindows.Others(windows, current), current);
        foreach (var (app, item) in _perApp)
        {
            var of = AxitWindows.Of(windows, app);
            item.Text = $"{AxitWindows.AppLabel(app)} ({of.Count})";
            FillWindows(item, of, current);
        }
    }

    private static void FillWindows(ToolStripMenuItem list, IReadOnlyList<AxitWindow> windows, nint current)
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

        NoneWhenEmpty(list);
    }

    private static void NoneWhenEmpty(ToolStripMenuItem list)
    {
        if (list.DropDownItems.Count == 0)
        {
            list.DropDownItems.Add(new ToolStripMenuItem("(none)") { Enabled = false });
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
    public static void Start(string verb, string? path)
    {
        if (Environment.ProcessPath is not { } exe)
        {
            return;
        }

        var info = new ProcessStartInfo(exe) { UseShellExecute = false };
        info.ArgumentList.Add(verb);
        if (path is not null)
        {
            info.ArgumentList.Add(path);
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
}
