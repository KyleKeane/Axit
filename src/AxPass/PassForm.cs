using System.Runtime.InteropServices;
using AxPass.Core;
using Axit.Core;
using Axit.Forms;

namespace AxPass;

/// <summary>
/// axPass's window (docs/axpass/SPEC.md; the shape is docs/axit/item-apps.md's), on the item apps' shared window
/// <see cref="ItemForm"/>. This class holds what is axPass's: one KeePass database, its groups and entries from
/// KeePassXC's tool (<see cref="KeePassCli"/>), the master password held while unlocked, the locks, the menus and its
/// own keys. The commands are in <c>PassForm.Commands.cs</c>. Every run of the tool goes off the UI thread through
/// <c>Cli</c>, one at a time.
/// </summary>
public sealed partial class PassForm : ItemForm
{
    private readonly PassSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _groupMenu = new("&Group");
    private readonly ToolStripMenuItem _entryMenu = new("&Entry");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly System.Windows.Forms.Timer _lockTimer = new() { Interval = 30_000 };

    /// <summary>Clears a copied password after <c>ClipboardSeconds</c> (PW-5.4).</summary>
    private readonly System.Windows.Forms.Timer _clipboardTimer = new() { Interval = 30_000 };

    /// <summary>The clipboard's sequence number right after axPass put a password there; null when none is waiting to be cleared.</summary>
    private uint? _clipboardSequence;

    /// <summary>The entries' fields read so far, by path; emptied when the window locks.</summary>
    private readonly Dictionary<string, ItemDocument> _entries = new(StringComparer.Ordinal);

    private readonly HashSet<string> _reading = new(StringComparer.Ordinal);

    /// <summary>The database open, and its master password while unlocked (PW-D2): null when locked.</summary>
    private string? _database;

    private string? _password;
    private PassTree? _tree;

    /// <summary>The group shown: empty for the top.</summary>
    private string _group = string.Empty;

    /// <summary>Set while a command of the tool runs: a second waits for it to end.</summary>
    private bool _busy;

    /// <summary>Whether this unlock's backup is made (PW-4.5): the first change after unlocking copies the file first.</summary>
    private bool _backedUp;

    public PassForm(string? path, PassSettings settings, string? settingsError)
        : base(AxPassApp.Name, settings, PassSettings.DefaultPath, settingsError)
    {
        _settings = settings;

        // ---- menus and keys: one command list (docs/axit/app-kit.md AK-2), each key from the tables (PW-7) ----
        // The shared keys (ItemKeys) are ItemForm's commands, registered after these by PlaceControls; the items here
        // for them only show them. A key in a second menu is an item showing it too: its command is the first menu's.
        // Enter on a locked window unlocks it, before Enter opens (ItemForm's, later on the list).
        AddKey(new Command("Unlock", ItemKeys.Open, UnlockOrOpen) { When = () => _password is null && InNavigator(), Mark = CommandMark.Open });
        var file = new ToolStripMenuItem("&File");
        Add(file, new Command("&Open database...", AxPassKeys.OpenDatabase, ChooseDatabase) { When = NotRenaming, Mark = CommandMark.Open });
        file.DropDownItems.Add(new ToolStripMenuItem("&New database...", null, (_, _) => NewDatabase()));
        Add(file, new Command("&Lock", AxPassKeys.Lock, Lock) { When = NotRenaming });
        file.DropDownItems.Add(new ToolStripMenuItem("&Back up now", null, (_, _) => Do(BackUpNowAsync)));
        file.DropDownItems.Add(new ToolStripMenuItem("Show backups in ax&File", null, (_, _) => ShowBackups()));
        file.DropDownItems.Add(Item("&Refresh", () => Do(ReloadAsync), ItemKeys.Refresh));
        file.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        _groupMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open));
        _groupMenu.DropDownItems.Add(Item("New &entry...", NewItem, ItemKeys.New));
        _groupMenu.DropDownItems.Add(Item("New &group...", NewList, ItemKeys.NewList));
        _groupMenu.DropDownItems.Add(Item("To the &Recycle Bin", () => WithRow(DeleteRow), ItemKeys.Delete));

        _entryMenu.DropDownItems.Add(Item("&Open its fields", OpenSelected, ItemKeys.Open));
        Add(_entryMenu, new Command("Copy &user name", AxPassKeys.CopyUserName, CopyUserName) { When = NotRenaming, Mark = CommandMark.Act });
        Add(_entryMenu, new Command("Copy &password", AxPassKeys.CopyPassword, CopyPassword) { When = NotRenaming, Mark = CommandMark.Act });
        Add(_entryMenu, new Command("Open U&RL", AxPassKeys.OpenUrl, OpenEntryUrl) { When = NotRenaming, Mark = CommandMark.Act });
        _entryMenu.DropDownItems.Add(Item("Read in &full", ReadAll, ItemKeys.ReadAll));
        _entryMenu.DropDownItems.Add(Item("New &entry...", NewItem, ItemKeys.New));
        _entryMenu.DropDownItems.Add(Item("Rena&me", RenameSelected, ItemKeys.Rename));
        Add(_entryMenu, new Command("Move to a gr&oup...", AxPassKeys.MoveEntry, MoveEntry) { When = InNavigator });
        _entryMenu.DropDownItems.Add(Item("To the Recycle &Bin", () => WithRow(DeleteRow), ItemKeys.Delete));

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, ItemKeys.Open));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _fieldMenu.DropDownItems.Add(Item("Copy &user name", CopyUserName, AxPassKeys.CopyUserName));
        _fieldMenu.DropDownItems.Add(Item("Copy &password", CopyPassword, AxPassKeys.CopyPassword));

        var lockAfter = new ToolStripMenuItem("&Lock after");
        foreach (var minutes in new[] { 5, 10, 30, 60, 0 })
        {
            var item = new ToolStripMenuItem(minutes == 0 ? "&Never" : $"&{minutes} minutes") { Checked = settings.LockMinutes == minutes, Tag = minutes };
            item.Click += (_, _) =>
            {
                _settings.LockMinutes = minutes;
                SaveSettings();
                foreach (ToolStripMenuItem other in lockAfter.DropDownItems)
                {
                    other.Checked = (int)other.Tag! == minutes;
                }

                Announce(minutes == 0 ? "axPass locks only on Ctrl+L" : $"axPass locks after {minutes} minutes without a key", true);
            };
            lockAfter.DropDownItems.Add(item);
        }

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(lockAfter);
        options.DropDownItems.Add(new ToolStripMenuItem("&KeePassXC's tool...", null, (_, _) => ChooseCli()));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "KeePass &resources",
            [("KeePass&XC", "https://keepassxc.org/"), ("The KDBX &format", "https://keepass.info/help/kb/kdbx.html")]);

        _menu.Items.AddRange([file, _groupMenu, _entryMenu, _fieldMenu, options, CreateAxitMenu(), help]);
        PlaceControls(_menu);

        _lockTimer.Tick += (_, _) => LockWhenIdle();
        _clipboardTimer.Tick += (_, _) => ClearCopiedPassword();
        ApplyTextFont();
        RestoreWindow();
        _database = path is not null && File.Exists(path) ? Path.GetFullPath(path)
            : _settings.LastDatabase is { } last && File.Exists(last) ? last : null;
        Fill(select: null);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IReadOnlyList<string> RestartArguments() => _database is null ? [Dispatch.AxPass] : [Dispatch.AxPass, _database];

    // Never the password, nor anything of the database but whether one is open.
    protected override IEnumerable<string> DiagnosticsLines => [$"A database open: {_database is not null}, unlocked: {_password is not null}, the tool found: {ToolPath() is not null}"];

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _lockTimer.Start();
        // PW-4.3, security audit S-18: locked when Windows locks, the user switches or disconnects, and on sleep.
        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
        if (ToolPath() is null)
        {
            ShowError("KeePassXC is not installed", "axPass reads and writes KeePass databases through KeePassXC's command-line tool, which comes with KeePassXC.\r\n\r\nInstall KeePassXC from https://keepassxc.org/, or in a console: winget install KeePassXCTeam.KeePassXC\r\n\r\nThen start axPass again. If KeePassXC is installed somewhere else, Options, KeePassXC's tool tells axPass where.");
        }
        else if (_database is not null)
        {
            ShowUnlock(null);
        }
    }

    /// <summary>The database, for Recently closed (AX-9.4): its path only.</summary>
    protected override string? PathInHand => _database;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _password = null;
        _lockTimer.Stop();
        ClearCopiedPassword();
        Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;
        Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        base.OnFormClosing(e);
    }

    private void OnSessionSwitch(object? sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        if (e.Reason is Microsoft.Win32.SessionSwitchReason.SessionLock or Microsoft.Win32.SessionSwitchReason.ConsoleDisconnect
            or Microsoft.Win32.SessionSwitchReason.RemoteDisconnect or Microsoft.Win32.SessionSwitchReason.SessionLogoff)
        {
            LockFromWindows("Windows locked");
        }
    }

    private void OnPowerModeChanged(object? sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
        {
            LockFromWindows("the computer went to sleep");
        }
    }

    /// <summary>Windows' events come on a thread of their own: the lock runs on the window's.</summary>
    private void LockFromWindows(string reason)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (_password is not null)
            {
                Log.Info($"Locked: {reason}");
                Lock();
            }
        });
    }

    /// <summary>Enter in the navigator of a locked window: the unlock notice, or the database chosen when none is open.</summary>
    private void UnlockOrOpen()
    {
        if (_database is null)
        {
            ChooseDatabase();
        }
        else
        {
            ShowUnlock(null);
        }
    }

    protected override char MenuLetter(bool inDetail, ItemRow? row) => inDetail ? 'D' : row is { IsList: false } ? 'E' : 'G';

    protected override void UpdateRowMenus(bool inDetail, ItemRow? row)
    {
        var unlocked = _password is not null;
        var onEntry = row is { IsList: false };
        _fieldMenu.Enabled = unlocked && inDetail;
        _entryMenu.Enabled = unlocked && !inDetail && onEntry;
        _groupMenu.Enabled = unlocked && !inDetail && !onEntry;
    }

    /// <summary>PW-6.1: KeePass's own fields only.</summary>
    protected override string? FieldsFixed => "An entry has KeePass's own fields here; other fields are added in KeePassXC's own window";

    // ---- the levels ----

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        if (_database is null)
        {
            return ([], "No database open: Ctrl+O opens one, File, New database makes one", "No database", string.Empty);
        }

        var name = PassTree.DatabaseName(_database);
        if (_password is null || _tree is null)
        {
            return ([], $"{name}, locked: Enter unlocks", name, _database);
        }

        if (!_tree.HasGroup(_group))
        {
            _group = string.Empty;
        }

        var rows = new List<ItemRow>();
        foreach (var node in _tree.Level(_group))
        {
            if (node.IsGroup)
            {
                rows.Add(new ItemRow(true, GroupKey(node.Path), node.Name, $"{node.Name}, group, {Count(_tree.CountIn(node.Path), "item")}", node));
            }
            else
            {
                var user = _entries.TryGetValue(node.Path, out var fields) && EntryFields.ValueOf(fields, EntryFields.UserName) is { Length: > 0 } u ? ", " + u : string.Empty;
                rows.Add(new ItemRow(false, node.Path, node.Name, node.Name + user, node));
            }
        }

        var level = _group.Length == 0 ? name : _group[(_group.LastIndexOf('/') + 1)..];
        var count = rows.Count == 0 ? "empty: Ctrl+N adds an entry" : Count(rows.Count, "item");
        return (rows, $"{level}, {count}", _group.Length == 0 ? name : $"{_group.Replace("/", ", ")}, {name}", _database);
    }

    /// <summary>A group's key: its path with the slash the tool gives a group, so that a group and an entry of one name differ.</summary>
    private static string GroupKey(string path) => path + "/";

    protected override void EnterList(ItemRow row) => _group = ((PassNode)row.Tag!).Path;

    protected override string? LeaveList()
    {
        if (_group.Length == 0)
        {
            return null;
        }

        var select = GroupKey(_group);
        var slash = _group.LastIndexOf('/');
        _group = slash < 0 ? string.Empty : _group[..slash];
        return select;
    }

    // ---- the detail pane ----

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row)
        {
            case null:
                return new ItemDetail("Details", null, null, [new ItemDetailLine(Navigator.AccessibleName ?? string.Empty)]);
            case { IsList: true, Tag: PassNode group }:
                return new ItemDetail("Details of " + group.Name, row.Key, null,
                [
                    new ItemDetailLine($"A group of {Count(_tree?.CountIn(group.Path) ?? 0, "item")}"),
                    new ItemDetailLine(PassTree.InRecycleBin(group.Path) ? "In the Recycle Bin: Delete removes for good" : "A group is renamed in KeePassXC's own window"),
                ]);
            case { Tag: PassNode entry }:
                if (_entries.TryGetValue(entry.Path, out var fields))
                {
                    return new ItemDetail("Fields of " + entry.Name, entry.Path, fields, [], entry.Path);
                }

                ReadEntry(entry.Path);
                return new ItemDetail("Fields of " + entry.Name, entry.Path, null, [new ItemDetailLine("Reading the entry")]);
            default:
                return new ItemDetail("Details", null, null, []);
        }
    }

    /// <summary>An entry's fields read off the UI thread (PW-5.3) and the detail pane filled again when it is still the one selected.</summary>
    private async void ReadEntry(string path)
    {
        if (_password is not { } password || _database is not { } database || ToolPath() is not { } cli || !_reading.Add(path))
        {
            return;
        }

        try
        {
            var result = await Task.Run(() => KeePassCli.Run(cli, KeePassCli.On("show", database, null, path), [password]));
            if (_password is null || _database != database)
            {
                return;
            }

            if (result.Succeeded)
            {
                _entries[path] = EntryFields.Parse(result.Output);
                if (SelectedKey() == path)
                {
                    var index = Navigator.SelectedIndex;
                    Fill(path);
                    if (Navigator.SelectedIndex != index && index >= 0 && index < Navigator.Items.Count)
                    {
                        Navigator.SelectedIndex = index;
                    }
                }
            }
            else
            {
                Log.Error("An entry could not be read: " + result.Why);
            }
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Error("An entry could not be read", ex);
        }
        finally
        {
            _reading.Remove(path);
        }
    }

    // ---- the password, the locks ----

    /// <summary>The tool's path; null when KeePassXC is not installed.</summary>
    private string? ToolPath() => KeePassCli.Find(_settings.CliPath);

    /// <summary>PW-4.2: the master password in a hidden field; <paramref name="refused"/> says the last one did not open it.</summary>
    private void ShowUnlock(string? refused)
    {
        if (_database is not { } database)
        {
            return;
        }

        var name = PassTree.DatabaseName(database);
        ShowNotice(new Notice("Unlock " + name, (refused is null ? string.Empty : refused + "\r\n\r\n") + database + ".",
        [
            new OverlayChoice("&Unlock", () =>
            {
                var typed = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                Do(() => UnlockAsync(database, typed));
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            // The field says what to do, since every password field reads the same otherwise (PW-6.7).
            Input = new OverlayInput("Master password of " + name + ":", "Type the master password to unlock " + name, string.Empty, "Type the master password", Hidden: true),
        });
    }

    private async Task UnlockAsync(string database, string password)
    {
        if (ToolPath() is not { } cli)
        {
            return;
        }

        Announce("Unlocking " + PassTree.DatabaseName(database), true);
        var result = await Task.Run(() => KeePassCli.Run(cli, KeePassCli.On("ls", database, ["-R", "-f"]), [password]));
        if (!result.Succeeded)
        {
            ShowUnlock(result.Error.Trim().Length == 0 ? $"The password does not open {PassTree.DatabaseName(database)}." : result.Why);
            return;
        }

        _database = database;
        _password = password;
        _backedUp = false;
        _tree = PassTree.Parse(result.Output);
        _group = string.Empty;
        _entries.Clear();
        _settings.LastDatabase = database;
        SaveSettings();
        Fill(select: null);
        Navigator.Select();
        AnnounceLevel();
    }

    /// <summary>Ctrl+L (PW-4.3): the password and everything read dropped, the unlock notice shown.</summary>
    private void Lock()
    {
        if (_password is null)
        {
            if (_database is not null)
            {
                ShowUnlock(null);
            }

            return;
        }

        _password = null;
        _tree = null;
        _entries.Clear();
        _undo.Clear();
        ClearCopiedPassword();
        Fill(select: null);
        Announce("Locked", true);
        ShowUnlock(null);
    }

    /// <summary>PW-4.3: locked after the minutes without a key or a mouse move anywhere in Windows.</summary>
    private void LockWhenIdle()
    {
        if (_password is null || _settings.LockMinutes == 0)
        {
            return;
        }

        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (GetLastInputInfo(ref info) && (uint)Environment.TickCount - info.Time >= (uint)_settings.LockMinutes * 60_000)
        {
            Log.Info("Locked after the idle minutes");
            Lock();
        }
    }

    /// <summary>The whole list read again from the database (F5, and after a change), the selection on <paramref name="select"/>.</summary>
    private async Task ReloadAsync() => await ReloadAsync(SelectedKey());

    private async Task ReloadAsync(string? select)
    {
        if (await Cli("ls", ["-R", "-f"]) is { } result)
        {
            _tree = PassTree.Parse(result.Output);
            Fill(select);
        }
    }

    /// <summary>
    /// One command of the tool on the open database, off the UI thread, with the master password first on its input
    /// and <paramref name="input"/> after it; null after saying why it did not run or failed.
    /// </summary>
    private async Task<CliResult?> Cli(string command, IEnumerable<string>? options, string[]? rest = null, string[]? input = null)
    {
        if (_password is not { } password || _database is not { } database)
        {
            Announce("Locked: Enter unlocks", true);
            return null;
        }

        if (ToolPath() is not { } cli)
        {
            Announce("KeePassXC's tool is not there any more", true);
            return null;
        }

        if (_busy)
        {
            Announce("Still working on the last command", true);
            return null;
        }

        _busy = true;
        try
        {
            if (Backups.Changes(command) && !_backedUp)
            {
                // PW-4.5: the file as it was, copied before the first change after unlocking; no change without it.
                try
                {
                    await Task.Run(() => Backups.Make(database, DateTime.Now));
                    _backedUp = true;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Announce("No backup could be made, so nothing changed: " + ex.Message, true);
                    return null;
                }
            }

            var result = await Task.Run(() => KeePassCli.Run(cli, KeePassCli.On(command, database, options, rest ?? []), [password, .. input ?? []]));
            if (!result.Succeeded)
            {
                Announce(result.Why, true);
                return null;
            }

            return result;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>File, Back up now (PW-4.5): a copy of the database in its backup folder at once.</summary>
    private async Task BackUpNowAsync()
    {
        if (_database is not { } database)
        {
            Announce("No database open", true);
            return;
        }

        try
        {
            var copy = await Task.Run(() => Backups.Make(database, DateTime.Now));
            Announce($"Backed up: {Path.GetFileName(copy)}, in {Path.GetFileName(Backups.FolderFor(database))}", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The backup could not be made: " + ex.Message);
        }
    }

    /// <summary>File, Show backups in axFile: the backup folder, where a backup opens like any database (Ctrl+O).</summary>
    private void ShowBackups()
    {
        if (_database is not { } database)
        {
            Announce("No database open", true);
        }
        else if (!Directory.Exists(Backups.FolderFor(database)))
        {
            Announce("No backups yet: the first change after unlocking makes one, or File, Back up now", true);
        }
        else
        {
            AxitMenu.Start(Dispatch.AxFile, Backups.FolderFor(database));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);
}
