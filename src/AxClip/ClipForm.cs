using System.Runtime.InteropServices;
using AxClip.Core;
using Axit.Core;
using Axit.Forms;
using static Axit.Forms.User32;

namespace AxClip;

/// <summary>
/// axClip's window (SPEC.md CP-2): a menu bar, one search field with the caret always in it, the list of items under
/// it that never takes the focus (as axStart's, AS-D2), and a status bar. The window listens to the clipboard for the
/// life of the process (CP-3) and is the bundle's background window (<see cref="HotkeyForm"/>): hidden until the
/// hotkey, hidden again on Enter, Escape and another program coming to the front, with the front given back.
/// </summary>
public sealed class ClipForm : HotkeyForm
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;


    /// <summary>How long after the window hid a paste waits for the program to be in front again (CP-4.1).</summary>
    private const int PasteDelayMs = 150;

    /// <summary>A clipboard another program holds open is read again this often, at most <see cref="ReadAttempts"/> times (CP-3.3).</summary>
    private const int RetryMs = 100;

    private const int ReadAttempts = 3;

    private readonly ClipSettings _settings;
    private readonly ClipHistory _history;
    private readonly MenuStrip _menu = new();

    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _countLabel;
    private readonly ToolStripStatusLabel _recordingLabel;
    private readonly ToolStripMenuItem _pauseItem;

    private IReadOnlyList<ClipEntry> _matches = [];
    private bool _paused;

    public ClipForm(bool hidden, ClipSettings settings, string? settingsError)
        : base(AxClipApp.Name, settings, ClipSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;
        _history = new ClipHistory(settings.History);

        // ---- menus (CP-5.1); the keys are handled in ProcessWindowKey from the table, the items only show them ----
        var clip = new ToolStripMenuItem("ax&Clip");
        clip.DropDownItems.Add(Item("&Paste", Paste, AxClipKeys.Paste));
        clip.DropDownItems.Add(Item("&Copy without pasting", CopyOnly, AxClipKeys.CopyOnly));
        clip.DropDownItems.Add(Item("&Read in full", ReadInFull, AxClipKeys.ReadInFull));
        clip.DropDownItems.Add(Item("Re&move", RemoveSelected, AxClipKeys.Remove));
        clip.DropDownItems.Add(Item("C&lear history...", ClearHistory, AxClipKeys.ClearHistory));
        clip.DropDownItems.Add(new ToolStripSeparator());
        clip.DropDownItems.Add(Item("&Hide", () => HideWindow(), AxClipKeys.Hide));
        clip.DropDownItems.Add(new ToolStripMenuItem("&Quit axClip", null, (_, _) => Quit()));

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(new ToolStripMenuItem("&Hotkey...", null, (_, _) => ShowHotkeyNotice()));
        _pauseItem = Item("&Pause recording", TogglePause, AxClipKeys.PauseRecording);
        options.DropDownItems.Add(_pauseItem);
        options.DropDownItems.Add(Toggle("&Keep history between runs", settings.KeepHistory, v =>
        {
            // CP-3.4: off, the file's history is emptied by the save that follows.
            _settings.KeepHistory = v;
            _settings.Keep(_history.Items);
        }));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(
            Toggle("&Start with Windows", _settings.StartWithWindows, v =>
            {
                _settings.StartWithWindows = v;
                SignInStart.Apply(AxClipApp.Name, Dispatch.AxClip, v);
            }),
            Toggle("Say the first &match while typing", _settings.SayFirstMatch, v => _settings.SayFirstMatch = v)));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Windows &resources",
            [("&Clipboard in Windows", "https://support.microsoft.com/windows/clipboard-in-windows-c436501e-985d-1c8d-97ea-fe46ddf338c6")]);

        _menu.Items.AddRange([clip, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the field and the items (CP-2.1, AS-D2): HotkeyForm's, named here ----
        SearchField.AccessibleName = "Search the clipboard";
        Matches.DoubleClick += (_, _) => Paste();

        (_countLabel, _recordingLabel) = StatusLayout.Pair(_status);

        Controls.AddRange([Overlay, Matches, SearchField, _status, _menu]);
        ApplyTextFont();
        Fill();
    }

    // ---- what the window bases ask for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        SearchField.Visible = false;
        Matches.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        SearchField.Visible = true;
        Matches.Visible = true;
    }

    protected override Control FocusTarget => SearchField;

    protected override void ApplyFont(Font font)
    {
        SearchField.Font = font;
        Matches.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxClip];

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Items: {_history.Items.Count}, {(_paused ? "paused" : "recording")}, history {(_settings.KeepHistory ? "kept" : "in memory")}",
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
    ];

    protected override Hotkey CurrentHotkey => _settings.Key;

    protected override void KeepHotkey(Hotkey key)
    {
        _settings.HotkeyText = key.ToString();
        SaveSettings();
    }

    /// <summary>The field emptied and the window centred on the screen of the program that had the focus (CP-2.2).</summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            SearchField.Clear();
        }

        Fill();
        var width = Math.Max(MinimumSize.Width, area.Width / 3);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + SearchField.Height + Matches.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            SearchField.Select();
        }
    }

    /// <summary>As the window hides: the timer stopped, and the history written when it is kept (CP-3.4).</summary>
    protected override void OnHiding()
    {
        if (_settings.KeepHistory)
        {
            _settings.Keep(_history.Items);
            SaveSettings();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SignInStart.Apply(AxClipApp.Name, Dispatch.AxClip, _settings.StartWithWindows);
        if (!AddClipboardFormatListener(Handle))
        {
            Log.Error("The clipboard listener could not be added");
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        RemoveClipboardFormatListener(Handle);
        base.OnHandleDestroyed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _settings.Keep(_history.Items);
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            BeginInvoke(() => ReadClipboard(1));
        }

        base.WndProc(ref m);
    }

    // ---- keys (CP-5.2): every key comes from BundleKeys or AxClipKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        if (ProcessMatchKey(keyData, AxClipKeys.NextMatch, AxClipKeys.PreviousMatch, AxClipKeys.FirstMatch, AxClipKeys.LastMatch))
        {
            return true;
        }

        switch (keyData)
        {
            case AxClipKeys.Paste:
                Paste();
                return true;
            case AxClipKeys.CopyOnly:
                CopyOnly();
                return true;
            case AxClipKeys.ReadInFull:
                ReadInFull();
                return true;
            case AxClipKeys.Remove:
                RemoveSelected();
                return true;
            case AxClipKeys.ClearHistory:
                ClearHistory();
                return true;
            case AxClipKeys.PauseRecording:
                TogglePause();
                return true;
            case AxClipKeys.Hide:
                Log.Info("Hidden: Escape");
                HideWindow();
                return true;
        }

        return false;
    }

    // ---- what is kept (CP-3) ----

    /// <summary>
    /// A change of the clipboard: its text kept when the policy says so (CP-3.1), the list made again; a clipboard
    /// another program holds open read again a little later (CP-3.3). Nothing is said: a copy is the user's own act
    /// in another program.
    /// </summary>
    private void ReadClipboard(int attempt)
    {
        string? text;
        bool excluded;
        int? historyValue;
        try
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            excluded = ClipPolicy.ExcludingFormats.Any(format => IsClipboardFormatAvailable(RegisterClipboardFormat(format)));
            historyValue = HistoryValue();
            text = Clipboard.GetText();
        }
        catch (ExternalException)
        {
            if (attempt < ReadAttempts)
            {
                Do(async () =>
                {
                    await Task.Delay(RetryMs);
                    ReadClipboard(attempt + 1);
                });
            }
            else
            {
                Log.Info("The clipboard could not be read");
            }

            return;
        }

        if (!ClipPolicy.Keeps(text, excluded, historyValue, _paused))
        {
            return;
        }

        var keep = Selected?.Text;
        _history.Add(text!, DateTime.Now);
        Fill(keep);
    }

    /// <summary>
    /// The value of <c>CanIncludeInClipboardHistory</c> when the copy carries it, a number in its first four bytes (0
    /// keeps the copy out); null without it. Read through the clipboard API, since WinForms' GetData is obsolete in
    /// .NET 10; a clipboard held open by another program throws, as Clipboard's own reads do, for the retry.
    /// </summary>
    private int? HistoryValue()
    {
        var format = RegisterClipboardFormat(ClipPolicy.HistoryFormat);
        if (!IsClipboardFormatAvailable(format))
        {
            return null;
        }

        if (!OpenClipboard(Handle))
        {
            throw new ExternalException("The clipboard is open in another program");
        }

        try
        {
            var data = GetClipboardData(format);
            var pointer = data == 0 ? 0 : GlobalLock(data);
            if (pointer == 0)
            {
                return null;
            }

            try
            {
                return Marshal.ReadInt32(pointer);
            }
            finally
            {
                GlobalUnlock(data);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private void TogglePause()
    {
        _paused = !_paused;
        _pauseItem.Checked = _paused;
        UpdateStatus();
        Announce(_paused ? "Recording paused" : "Recording", true);
    }

    // ---- the items (CP-2.3, CP-2.4) ----

    private ClipEntry? Selected => Matches.SelectedIndex >= 0 && Matches.SelectedIndex < _matches.Count ? _matches[Matches.SelectedIndex] : null;

    protected override int MatchCount => _matches.Count;

    protected override string SpokenMatch(int index) => ClipHistory.Spoken(_matches[index], index, _matches.Count);

    protected override string NothingSaid => _history.Nothing(SearchField.Text);

    protected override bool SayFirstMatch => _settings.SayFirstMatch;

    protected override void Refill() => Fill();

    /// <summary>The list made again from the typed text, the selection on <paramref name="keep"/> when it is still there, the newest otherwise.</summary>
    private void Fill(string? keep = null)
    {
        _matches = _history.Find(SearchField.Text);
        var index = keep is null ? 0 : Math.Max(0, _matches.ToList().FindIndex(entry => entry.Text == keep));
        ShowMatches(_matches.Select(entry => entry.Label).ToList(), _history.Nothing(SearchField.Text), $"Clipboard, {_matches.Count}", index);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var count = _history.Items.Count;
        _countLabel.Text = count == 1 ? "1 item" : $"{count} items";
        _recordingLabel.Text = _paused ? "Paused" : "Recording";
        StatusLayout.Fit(_status, _countLabel, _recordingLabel);
    }


    // ---- using an item (CP-4) ----

    /// <summary>Enter (CP-4.1): the item on the clipboard, the window hidden with the front given back, then Ctrl+V once that program is in front again.</summary>
    private void Paste()
    {
        if (Selected is not { } entry)
        {
            Announce(_history.Nothing(SearchField.Text), true);
            return;
        }

        if (!CopyText(entry.Text, null))
        {
            return;
        }

        var target = ForegroundBefore;
        Log.Info("Hidden: a paste");
        HideWindow();
        Do(async () =>
        {
            await Task.Delay(PasteDelayMs);
            if (target != 0 && GetForegroundWindow() == target)
            {
                SendCtrlV();
                Log.Info("Pasted");
            }
            else
            {
                // CP-D3: nothing typed into a window the user did not come from; the text waits on the clipboard.
                Log.Info("The paste was left undone: another window is in front");
            }
        });
    }

    /// <summary>Shift+Enter (CP-4.2).</summary>
    private void CopyOnly()
    {
        if (Selected is not { } entry)
        {
            Announce(_history.Nothing(SearchField.Text), true);
            return;
        }

        if (CopyText(entry.Text, "Copied"))
        {
            HideWindow();
        }
    }

    /// <summary>Ctrl+Q (CP-4.3): the whole text in a notice.</summary>
    private void ReadInFull()
    {
        if (Selected is not { } entry)
        {
            Announce(_history.Nothing(SearchField.Text), true);
            return;
        }

        var lines = entry.LineCount;
        ShowNotice(Notice.Plain($"Clipboard text, {lines} {(lines == 1 ? "line" : "lines")}", entry.Text));
    }

    private void RemoveSelected()
    {
        if (Selected is not { } entry)
        {
            Announce(_history.Nothing(SearchField.Text), true);
            return;
        }

        var index = Matches.SelectedIndex;
        _history.Remove(entry);
        Fill();
        if (_matches.Count > 0)
        {
            Matches.SelectedIndex = Math.Min(index, _matches.Count - 1);
        }

        Announce("Removed", true);
    }

    private void ClearHistory()
    {
        var count = _history.Items.Count;
        if (count == 0)
        {
            Announce("The clipboard history is empty", true);
            return;
        }

        ShowNotice(new Notice("Clear the clipboard history?", count == 1 ? "Its item cannot be brought back." : $"Its {count} items cannot be brought back.",
        [
            new OverlayChoice("C&lear", () =>
            {
                _history.Clear();
                _settings.Keep(_history.Items);
                SaveSettings();
                // S-21: a copy of an unreadable settings file kept for mending would still hold the old history.
                try
                {
                    File.Delete(SettingsPath + ".bad");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }

                Fill();
                Announce("Clipboard history cleared", true);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    // ---- the paste's keys (CP-D3) ----

    private const uint InputKeyboard = 1;
    private const uint KeyEventUp = 0x0002;
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;

    /// <summary>Ctrl+V to the program in front: Ctrl down, V down, V up, Ctrl up, in one <c>SendInput</c>.</summary>
    private static void SendCtrlV()
    {
        Input[] inputs =
        [
            Key(VkControl, 0),
            Key(VkV, 0),
            Key(VkV, KeyEventUp),
            Key(VkControl, KeyEventUp),
        ];
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            Log.Info("The paste's keys were not all sent");
        }
    }

    private static Input Key(ushort key, uint flags) => new() { Type = InputKeyboard, Keyboard = new KeyboardInput { Key = key, Flags = flags } };

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Key;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    /// <summary>INPUT with its keyboard member; the padding makes it as large as the union's largest member, the mouse's.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public KeyboardInput Keyboard;
        public long Padding;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterClipboardFormat(string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(nint owner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(nint memory);
}
