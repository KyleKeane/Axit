using System.ComponentModel;
using System.Diagnostics;
using AxProcess.Core;
using Axit.Core;
using Axit.Forms;

namespace AxProcess;

/// <summary>
/// axProcess's window (SPEC.md PR-2): a menu bar, one list with three views (Processes, Services, Startup) and a status
/// bar. Every read runs on a background task (AX-7.5) and nothing refreshes by itself (PR-D2); Enter shows an item's
/// details as a notice with what can be done to it. The notices, the fonts, the window's place, find and the update
/// flow are the bundle's window base (<see cref="AxitForm"/>).
/// </summary>
public sealed class ProcessForm : AxitForm
{
    private enum View
    {
        Processes,
        Services,
        Startup,
    }


    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    private readonly ProcessSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly AxitList _list = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _nameLabel;
    private readonly ToolStripStatusLabel _memoryLabel;
    /// <summary>The three views of the list (PR-2.2), as axGIT's.</summary>
    private readonly ListViewState[] _views = [new(), new(), new()];
    private readonly Dictionary<ProcessSort, ToolStripMenuItem> _sortItems = [];
    private readonly ToolStripMenuItem _processMenu = new("&Process");
    private readonly ToolStripMenuItem _serviceMenu = new("&Service");
    private readonly ToolStripMenuItem _startupMenu = new("S&tartup");

    private View _view = View.Processes;
    private IReadOnlyList<ProcessRow> _processes = [];
    private IReadOnlyList<ServiceRow> _services = [];
    private IReadOnlyList<StartupEntry> _startup = [];
    private double? _processor;

    /// <summary>What is being read or done now, for the status bar; null when nothing runs (PR-2.3).</summary>
    private string? _busy;

    private bool _announcedFirstRead;

    public ProcessForm(ProcessSettings settings, string? settingsError)
        : base(AxProcessApp.Name, settings, ProcessSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        Text = "This computer - " + AxProcessApp.Name;

        // ---- menus (PR-6.1); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // The first menu holds what every view has (AX-6: the thing worked on, then Exit); each view has a menu of its
        // own, and only the menu of the view shown is enabled, so that a process command is never at hand among the
        // services (the user's ask, 2026-10-24; UpdateViewMenus).
        var computer = new ToolStripMenuItem("&Computer");
        computer.DropDownItems.Add(Item("&Refresh", () => Do(() => RefreshAsync(announce: true)), AxProcessKeys.Refresh));
        computer.DropDownItems.Add(Item("&Summary", SaySummary, AxProcessKeys.Summary));
        computer.DropDownItems.Add(Item("&Copy line", CopyLine, AxProcessKeys.CopyLine));
        computer.DropDownItems.Add(new ToolStripSeparator());
        computer.DropDownItems.Add(CreateExitItem());

        _processMenu.DropDownItems.Add(Item("&Details", () => Do(ActAsync), AxProcessKeys.Act));
        _processMenu.DropDownItems.Add(Item("&End process...", () => EndProcess(tree: false), AxProcessKeys.End));
        _processMenu.DropDownItems.Add(new ToolStripMenuItem("End process &tree...", null, (_, _) => EndProcess(tree: true)));
        _processMenu.DropDownItems.Add(Item("Show program in ax&File", ShowProgram, AxProcessKeys.ShowProgram));

        _serviceMenu.DropDownItems.Add(Item("&Details", () => Do(ActAsync), AxProcessKeys.Act));
        _serviceMenu.DropDownItems.Add(new ToolStripMenuItem("&Start", null, (_, _) => ControlSelectedService(ServiceAction.Start)));
        _serviceMenu.DropDownItems.Add(new ToolStripMenuItem("S&top", null, (_, _) => ControlSelectedService(ServiceAction.Stop)));
        _serviceMenu.DropDownItems.Add(new ToolStripMenuItem("&Restart", null, (_, _) => ControlSelectedService(ServiceAction.Restart)));

        _startupMenu.DropDownItems.Add(Item("&Details", () => Do(ActAsync), AxProcessKeys.Act));
        _startupMenu.DropDownItems.Add(Item("Show program in ax&File", ShowProgram, AxProcessKeys.ShowProgram));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("&Processes", () => Do(() => ShowViewAsync(View.Processes)), AxProcessKeys.ViewProcesses));
        view.DropDownItems.Add(Item("&Services", () => Do(() => ShowViewAsync(View.Services)), AxProcessKeys.ViewServices));
        view.DropDownItems.Add(Item("Start&up", () => Do(() => ShowViewAsync(View.Startup)), AxProcessKeys.ViewStartup));
        view.DropDownItems.Add(new ToolStripSeparator());
        var sort = new ToolStripMenuItem("Sort processes &by");
        foreach (var (choice, text) in new[] { (ProcessSort.Memory, "&Memory"), (ProcessSort.Processor, "&Processor"), (ProcessSort.Name, "&Name") })
        {
            var item = new ToolStripMenuItem(text, null, (_, _) => SetSort(choice)) { Checked = settings.SortBy == choice };
            _sortItems[choice] = item;
            sort.DropDownItems.Add(item);
        }

        view.DropDownItems.Add(sort);

        // Options, as the other windows' (AX-6).
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Windows &resources",
        [
            ("&Task Manager", "https://learn.microsoft.com/en-us/windows/win32/taskman/task-manager"),
            ("&Services", "https://learn.microsoft.com/en-us/windows/win32/services/services"),
        ]);

        // The bundle's Axit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([computer, _processMenu, _serviceMenu, _startupMenu, view, options, CreateAxitMenu(), help]);
        UpdateViewMenus();
        MainMenuStrip = _menu;

        // ---- the list (PR-2.1) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = "Processes, reading";
        _list.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && _list.IndexFromPoint(e.Location) is var index && index >= 0)
            {
                _list.SelectedIndex = index;
            }
        };
        _list.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                OpenViewMenu();
            }
        };

        (_nameLabel, _memoryLabel) = StatusLayout.Pair(_status);

        Controls.AddRange([Overlay, _list, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        UpdateStatusBar();
    }

    private enum ServiceAction
    {
        Start,
        Stop,
        Restart,
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _list.Visible = false;

    protected override void ShowAfterNotice() => _list.Visible = true;

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font) => _list.Font = font;

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxProcess];

    protected override IEnumerable<string> DiagnosticsLines =>
        [$"View: {_view}, {Current.Labels.Count} items, sorted by {ProcessTable.SortName(_settings.SortBy)}"];

    /// <summary>Ctrl+S, the bundle's save key: nothing to save, and the keys that act said (PR-6.2).</summary>
    protected override void SaveCommand() => Announce("Nothing to save here. F5 refreshes, Delete ends a process", true);

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
        Do(() => RefreshAsync(announce: false));
    }


    // ---- keys (PR-6.2): every key comes from BundleKeys or AxProcessKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.Find:
                ShowFindInList(_list, () => Current.Labels);
                return true;
            case BundleKeys.FindNext:
                FindInList(_list, () => Current.Labels, 1);
                return true;
            case BundleKeys.FindPrevious:
                FindInList(_list, () => Current.Labels, -1);
                return true;
            case AxProcessKeys.ViewProcesses:
                Do(() => ShowViewAsync(View.Processes));
                return true;
            case AxProcessKeys.ViewServices:
                Do(() => ShowViewAsync(View.Services));
                return true;
            case AxProcessKeys.ViewStartup:
                Do(() => ShowViewAsync(View.Startup));
                return true;
            case AxProcessKeys.Refresh:
                Do(() => RefreshAsync(announce: true));
                return true;
            case AxProcessKeys.ShowProgram:
                ShowProgram();
                return true;
            case AxProcessKeys.CopyLine:
                CopyLine();
                return true;
            case AxProcessKeys.Summary:
                SaySummary();
                return true;
        }

        if (_list.Focused)
        {
            // Enter, Delete and the menu keys act in the list; returning true keeps it from dinging.
            switch (keyData)
            {
                case AxProcessKeys.Act:
                    Do(ActAsync);
                    return true;
                case AxProcessKeys.End:
                    EndProcess(tree: false);
                    return true;
                case AxProcessKeys.NextView:
                    Do(() => ShowViewAsync(Cycled(1)));
                    return true;
                case AxProcessKeys.PreviousView:
                    Do(() => ShowViewAsync(Cycled(-1)));
                    return true;
                case AxProcessKeys.ItemMenu:
                case AxProcessKeys.ItemMenuAlso:
                    OpenViewMenu();
                    return true;
            }
        }

        return false;
    }

    /// <summary>The view after or before the one shown, round from Startup to Processes (Tab, Shift+Tab; PR-2.2).</summary>
    private View Cycled(int step) => (View)(((int)_view + step + 3) % 3);

    /// <summary>Shift+F10, the Applications key, a right click (PR-6.1): the view's menu from the bar, Process, Service or Startup.</summary>
    private void OpenViewMenu() => OpenBarMenu(_view switch { View.Services => 'S', View.Startup => 'T', _ => 'P' });

    /// <summary>The menu of the view shown enabled, the other two not (PR-6.1): WinForms skips a disabled menu on the bar's arrows and the palette leaves its items out.</summary>
    private void UpdateViewMenus()
    {
        _processMenu.Enabled = _view == View.Processes;
        _serviceMenu.Enabled = _view == View.Services;
        _startupMenu.Enabled = _view == View.Startup;
    }

    // ---- the views (PR-2.2, PR-2.3) ----

    private ListViewState Current => _views[(int)_view];

    /// <summary>F5, the first showing and every command's end: the view shown read again on a background task, the list refilled on the item it had.</summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_busy is not null)
        {
            Announce("Working…", true);
            return;
        }

        RememberSelection();
        if (!await LoadAsync(_view))
        {
            return;
        }

        FillList();
        if (announce)
        {
            Announce("Refreshed: " + _list.AccessibleName, true);
        }
        else if (!_announcedFirstRead)
        {
            // The list was empty when the window took the focus; its name and first item are said once, not interrupting.
            _announcedFirstRead = true;
            Announce(_list.SelectedItem is string item ? $"{_list.AccessibleName}. {item}" : _list.AccessibleName ?? string.Empty, false);
        }
    }

    /// <summary>Reads one view into its record; false when another read runs.</summary>
    private async Task<bool> LoadAsync(View view)
    {
        if (_busy is not null)
        {
            Announce("Working…", true);
            return false;
        }

        _busy = "Reading…";
        UpdateStatusBar();
        try
        {
            var state = _views[(int)view];
            switch (view)
            {
                case View.Processes:
                    var (rows, processor) = await Task.Run(ReadProcesses);
                    _processor = processor;
                    _processes = ProcessTable.Sorted(rows, _settings.SortBy);
                    state.Set(ProcessTable.ListName(_processes.Count), _processes.Select(row => row.Label), _processes.Select(row => row.Id.ToString()));
                    break;
                case View.Services:
                    _services = await Task.Run(ServiceManager.List);
                    state.Set(ServiceRow.ListName(_services.Count), _services.Select(row => row.Label), _services.Select(row => row.Name));
                    break;
                default:
                    _startup = await Task.Run(StartupList.Read);
                    state.Set(StartupEntry.ListName(_startup.Count), _startup.Select(entry => entry.Label), _startup.Select(entry => entry.Key));
                    break;
            }

            return true;
        }
        finally
        {
            _busy = null;
            if (!IsDisposed)
            {
                UpdateStatusBar();
            }
        }
    }

    /// <summary>Two samples a second apart (PR-D3), with the whole computer's busy share over the same second.</summary>
    private static (IReadOnlyList<ProcessRow> Rows, double? Processor) ReadProcesses()
    {
        var timesBefore = SystemInfo.Times();
        var first = ProcessTable.Sample();
        var watch = Stopwatch.StartNew();
        Thread.Sleep(SampleInterval);
        var second = ProcessTable.Sample();
        var timesAfter = SystemInfo.Times();
        return (ProcessTable.Rows(first, second, watch.Elapsed, Environment.ProcessorCount), SystemTimes.Busy(timesBefore, timesAfter));
    }

    /// <summary>View → Processes, Services, Startup (Ctrl+1, 2, 3): the list refilled, read on the first visit, and its name said with the item the selection is on.</summary>
    private async Task ShowViewAsync(View view)
    {
        if (_busy is not null)
        {
            Announce("Working…", true);
            return;
        }

        RememberSelection();
        _view = view;
        if (!Current.Loaded)
        {
            if (!await LoadAsync(view))
            {
                return;
            }
        }

        FillList();
        var name = _list.AccessibleName ?? string.Empty;
        Announce(_list.SelectedItem is string item ? $"{name}. {item}" : name, true);
    }

    private void FillList()
    {
        UpdateViewMenus();
        Current.FillInto(_list);
        UpdateStatusBar();
    }

    private void RememberSelection() => Current.Remember(_list);

    private ProcessRow? CurrentProcess() =>
        _view == View.Processes && _list.SelectedIndex is var i && i >= 0 && i < _processes.Count ? _processes[i] : null;

    private ServiceRow? CurrentService() =>
        _view == View.Services && _list.SelectedIndex is var i && i >= 0 && i < _services.Count ? _services[i] : null;

    private StartupEntry? CurrentStartup() =>
        _view == View.Startup && _list.SelectedIndex is var i && i >= 0 && i < _startup.Count ? _startup[i] : null;

    /// <summary>Whether the list shows the view a command belongs to; otherwise which view first is said (PR-6.2).</summary>
    private bool InView(View view)
    {
        if (_view == view)
        {
            return true;
        }

        Announce(view == View.Processes ? "Processes view first: Ctrl+1" : "Services view first: Ctrl+2", true);
        return false;
    }

    /// <summary>View → Sort processes by (PR-3.2): the rows put in the new order without a new read.</summary>
    private void SetSort(ProcessSort sort)
    {
        _settings.SortBy = sort;
        foreach (var (choice, item) in _sortItems)
        {
            item.Checked = choice == sort;
        }

        SaveSettings();
        if (_views[(int)View.Processes].Loaded)
        {
            if (_view == View.Processes)
            {
                RememberSelection();
            }

            _processes = ProcessTable.Sorted(_processes, sort);
            _views[(int)View.Processes].Set(ProcessTable.ListName(_processes.Count), _processes.Select(row => row.Label), _processes.Select(row => row.Id.ToString()));
            if (_view == View.Processes)
            {
                FillList();
            }
        }

        Announce("Sorted by " + ProcessTable.SortName(sort), true);
    }

    // ---- the items (PR-3, PR-4, PR-5) ----

    /// <summary>Enter, or the Details items: the selected item's notice for the view shown.</summary>
    private async Task ActAsync()
    {
        switch (_view)
        {
            case View.Processes:
                if (CurrentProcess() is not { } row)
                {
                    Announce("Nothing selected", true);
                    return;
                }

                var lines = await Task.Run(() => SystemInfo.Details(row));
                ShowNotice(new Notice(row.Name, string.Join("\n", lines),
                [
                    new OverlayChoice("&End process...", () => EndProcess(tree: false)),
                    new OverlayChoice("Show program in ax&File", ShowProgram),
                    new OverlayChoice("&Close", IsDefault: true, IsCancel: true),
                ]));
                return;
            case View.Services:
                ShowServiceDetails();
                return;
            default:
                if (CurrentStartup() is not { } entry)
                {
                    Announce("Nothing selected", true);
                    return;
                }

                ShowNotice(new Notice(entry.Name, string.Join("\n", entry.Details),
                [
                    new OverlayChoice("Show program in ax&File", ShowProgram),
                    new OverlayChoice("&Close", IsDefault: true, IsCancel: true),
                ]));
                return;
        }
    }

    /// <summary>Delete, Process → End process, End process tree (PR-3.3): a question, then the end on a background task and a new read.</summary>
    private void EndProcess(bool tree)
    {
        if (!InView(View.Processes))
        {
            return;
        }

        if (CurrentProcess() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        if (row.Id == Environment.ProcessId)
        {
            Announce("That is this window: close it with Alt+F4", true);
            return;
        }

        var title = tree ? $"End {row.Name} and the processes it started?" : $"End {row.Name} (process {row.Id})?";
        ShowNotice(new Notice(title, "Anything unsaved in it is lost.",
        [
            new OverlayChoice(tree ? "End process &tree" : "&End process", () => Do(async () =>
            {
                var said = await Task.Run(() => Kill(row, tree));
                Announce(said, true);
                await RefreshAsync(announce: false);
            }), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>The end itself, off the UI thread: what to say about it. The ID must still be the same program's, since Windows reuses IDs.</summary>
    private static string Kill(ProcessRow row, bool tree)
    {
        try
        {
            using var process = Process.GetProcessById(row.Id);
            if (!process.ProcessName.Equals(row.Name, StringComparison.OrdinalIgnoreCase))
            {
                return $"{row.Name} has already ended";
            }

            process.Kill(entireProcessTree: tree);
            process.WaitForExit(5000);
            Log.Info($"Ended {row.Name} ({row.Id}){(tree ? " with its tree" : string.Empty)}");
            return $"Ended {row.Name}";
        }
        catch (ArgumentException)
        {
            return $"{row.Name} has already ended";
        }
        catch (InvalidOperationException)
        {
            return $"{row.Name} has already ended";
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return $"Could not end {row.Name}: access is denied. It may need administrator rights";
        }
        catch (Win32Exception ex)
        {
            return $"Could not end {row.Name}: {ex.Message}";
        }
    }

    /// <summary>Ctrl+E (PR-3.5, PR-5.3): the program file in axFile, selected in its folder.</summary>
    private void ShowProgram()
    {
        string? path;
        string name;
        switch (_view)
        {
            case View.Processes when CurrentProcess() is { } row:
                path = SystemInfo.ProgramPath(row.Id);
                name = row.Name;
                break;
            case View.Startup when CurrentStartup() is { } entry:
                path = CommandLine.ProgramOf(entry.Command);
                name = entry.Name;
                break;
            case View.Services:
                Announce("Processes view first: Ctrl+1", true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }

        if (path is null || !File.Exists(path))
        {
            Announce($"Windows does not say where {name}'s program is", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxFile, path);
    }

    private void CopyLine()
    {
        if (_list.SelectedItem is not string line)
        {
            Announce("Nothing selected", true);
            return;
        }

        CopyText(line, "Copied");
    }

    /// <summary>Ctrl+Q (PR-2.5): the processes of the last read, or counted now, the memory now, the processor of the last read.</summary>
    private void SaySummary()
    {
        var count = _views[(int)View.Processes].Loaded ? _processes.Count : Process.GetProcesses().Length;
        var (total, available) = SystemInfo.Memory();
        Announce(ProcessTable.Summary(count, total, available, _processor), true);
    }

    // ---- services (PR-4) ----

    private void ShowServiceDetails()
    {
        if (CurrentService() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        var choices = new List<OverlayChoice>();
        if (row.State == ServiceState.Stopped)
        {
            choices.Add(new OverlayChoice("&Start", () => ControlService(row, ServiceAction.Start)));
        }

        if (row.State == ServiceState.Running && row.CanStop)
        {
            choices.Add(new OverlayChoice("S&top", () => ControlService(row, ServiceAction.Stop)));
            choices.Add(new OverlayChoice("&Restart", () => ControlService(row, ServiceAction.Restart)));
        }

        choices.Add(new OverlayChoice("&Close", IsDefault: true, IsCancel: true));
        ShowNotice(new Notice(row.DisplayName, string.Join("\n", row.Details), choices));
    }

    private void ControlSelectedService(ServiceAction action)
    {
        if (!InView(View.Services))
        {
            return;
        }

        if (CurrentService() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        ControlService(row, action);
    }

    /// <summary>Start, Stop, Restart (PR-4.3) on a background task, with the wait for the new state, then a new read.</summary>
    private void ControlService(ServiceRow row, ServiceAction action) => Do(async () =>
    {
        if (_busy is not null)
        {
            Announce("Working…", true);
            return;
        }

        var verb = action switch { ServiceAction.Start => "start", ServiceAction.Stop => "stop", _ => "restart" };
        _busy = action switch { ServiceAction.Start => "Starting ", ServiceAction.Stop => "Stopping ", _ => "Restarting " } + row.DisplayName + "…";
        UpdateStatusBar();
        ServiceResult result;
        try
        {
            result = await Task.Run(() => action switch
            {
                ServiceAction.Start => ServiceManager.Start(row.Name),
                ServiceAction.Stop => ServiceManager.Stop(row.Name),
                _ => Restart(row.Name),
            });
        }
        finally
        {
            _busy = null;
            UpdateStatusBar();
        }

        Log.Info($"Service {row.Name} {verb}: {(result.Done ? "done" : result.Denied ? "denied" : result.TimedOut ? "timed out" : result.Error)}");
        Announce(result switch
        {
            { Done: true } => action switch { ServiceAction.Start => "Started ", ServiceAction.Stop => "Stopped ", _ => "Restarted " } + row.DisplayName,
            { TimedOut: true } => $"{row.DisplayName} is still {(action == ServiceAction.Stop ? "stopping" : "starting")}",
            { Denied: true } => $"Could not {verb} {row.DisplayName}: access is denied. Services need administrator rights to start and stop",
            _ => $"Could not {verb} {row.DisplayName}: {result.Error}",
        }, true);
        await RefreshAsync(announce: false);
    });

    /// <summary>A stop, then a start once the stop is done; a stop that did not finish is the answer.</summary>
    private static ServiceResult Restart(string name)
    {
        var stop = ServiceManager.Stop(name);
        return stop.Done ? ServiceManager.Start(name) : stop;
    }

    // ---- the status bar (PR-2.4) ----

    private void UpdateStatusBar()
    {
        _nameLabel.Text = _busy ?? _list.AccessibleName ?? string.Empty;
        var (total, available) = SystemInfo.Memory();
        _memoryLabel.Text = total > 0 ? $"Memory {Math.Round((total - available) * 100d / total):0}% in use" : string.Empty;
        StatusLayout.Fit(_status, _nameLabel, _memoryLabel);
    }
}
