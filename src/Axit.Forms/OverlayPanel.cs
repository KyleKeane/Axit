
using Axit.Core;

namespace Axit.Forms;

/// <summary>A button on a notice. Unless <see cref="StaysOpen"/> is set, the notice closes before the action runs.</summary>
public sealed record OverlayChoice(string Text, Action? Action = null, bool IsDefault = false, bool IsCancel = false, bool StaysOpen = false)
{
    /// <summary>The single button of a plain notice: Enter and Escape both close it.</summary>
    public static readonly OverlayChoice Close = new("&Close", IsDefault: true, IsCancel: true);
}

/// <summary>
/// A text field on a notice (Find): its label, its accessible name, the initial
/// text, and, when <paramref name="Required"/>, what to say when the default button is pressed while the field is
/// empty (the notice then stays open). <paramref name="KeyHook"/>, when given, is asked about each key in the field
/// first and takes it by returning true (AxFile's path completion on Down, Up and Right).
/// </summary>
public sealed record OverlayInput(string Label, string AccessibleName, string Initial, string EmptyMessage, bool Required = true, Func<Keys, bool>? KeyHook = null);

/// <summary>A recommended way to start Claude in the New session notice (FR-8.6): the radio button's text and the arguments it stands for.</summary>
public sealed record OverlayPreset(string Text, IReadOnlyList<string> Arguments);

/// <summary>
/// The New session part of a notice (FR-8.6): the folder (null when none is chosen yet) with a Choose folder button
/// under it, the presets as radio buttons with Custom last (<paramref name="Selected"/> equal to the number of
/// presets selects Custom), and the argument field that shows while Custom is selected.
/// </summary>
public sealed record OverlaySession(string? Folder, Action ChooseFolder, IReadOnlyList<OverlayPreset> Presets, int Selected, string CustomArguments)
{
    /// <summary>The caption of the radio button group: AxClaude's "How Claude starts", AxTerminal's "Shell".</summary>
    public string PresetsCaption { get; init; } = "How Claude starts";

    /// <summary>The accessible name of the free field under Custom.</summary>
    public string CustomLabel { get; init; } = "Custom arguments";

    /// <summary>The help line above the free field.</summary>
    public string CustomHelp { get; init; } = "Arguments for Claude Code, one or more on each line. Enter starts the session, Shift+Enter starts a new line.";
}

/// <summary>
/// What a notice says and offers, and nothing about how it is shown (D23): the title, the text, the buttons, at most
/// one of the text field and the New session controls, and whether the user opened it. Every notice goes through
/// <c>MainForm.ShowNotice</c>, which adds what all notices share: the key line made from the buttons, the chime and
/// the hold of a notice the user did not open, the focus, and the blocked window.
/// </summary>
public sealed record Notice(string Title, string Text, IReadOnlyList<OverlayChoice> Choices)
{
    public OverlayInput? Input { get; init; }

    public OverlaySession? Session { get; init; }

    /// <summary>
    /// Opened by something other than the user's own command: an update offer or an error at startup. It plays the
    /// notice chime and ignores the answering keys for a moment, since the user may be typing elsewhere when it appears.
    /// </summary>
    public bool Unprompted { get; init; }

    /// <summary>
    /// A progress bar under the text, for a download (bundle AX-3.2). The window moves it with
    /// <see cref="OverlayPanel.SetProgress"/>; NVDA reports a progress bar the way it reports every other one, by its
    /// own settings (beeps or percentages), so the notice says nothing of its own about the progress.
    /// </summary>
    public bool Progress { get; init; }

    /// <summary>A read-only text with a Close button: help texts, About and errors.</summary>
    public static Notice Plain(string title, string text) => new(title, text, [OverlayChoice.Close]);
}

/// <summary>
/// The window's own dialogs, drawn inside the window (D23). A notice or a help text takes the place of the
/// conversation and the bottom of the window until it is answered, so there is never a second window to lose. (What
/// Claude waits on goes to the control area instead, D33.) The panel holds a title, a read-only text, an optional
/// text field or the optional New session controls, and a row of buttons; it shows a <see cref="Notice"/>, which says
/// what, and the window decides the rest. The window hides the controls underneath, blocks its own shortcuts and the
/// menu, and routes Enter and Escape to the default and cancel buttons through its AcceptButton and CancelButton; Tab
/// moves between the text, the field or the session controls, and the buttons.
/// </summary>
public sealed class OverlayPanel : Panel
{
    private const string NoFolder = "(none chosen yet)";
    private const string ChooseFolderFirst = "Choose a folder first";

    /// <summary>
    /// How long a notice the user did not open ignores the keys that answer (Enter, Space): it can open while the user
    /// is typing a message, and the next keys were meant for the message field.
    /// </summary>
    private const int HoldMs = 1000;

    private readonly Label _title = new();
    private readonly TextBox _text = new();
    private readonly Panel _inputRow = new();
    private readonly Label _inputLabel = new();
    private readonly TextBox _input = new();
    private readonly FlowLayoutPanel _session = new();
    private readonly Button _chooseFolder = new();
    private readonly GroupBox _presetGroup = new();
    private readonly FlowLayoutPanel _presetList = new();
    private readonly FlowLayoutPanel _customRow = new();
    private readonly Label _customLabel = new();
    private readonly TextBox _custom = new();
    private readonly List<RadioButton> _presets = [];
    private readonly FlowLayoutPanel _buttons = new();
    private readonly ProgressBar _progress = new();
    private OverlayInput? _inputSpec;
    private OverlaySession? _sessionSpec;
    private long _holdUntil;
    private string? _sessionFolder;
    private string _body = string.Empty;

    public OverlayPanel()
    {
        Visible = false;
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Control;
        Padding = new Padding(8);
        TabStop = false;

        _title.AutoSize = true;
        _title.Dock = DockStyle.Top;
        _title.Font = new Font(SystemFonts.MessageBoxFont ?? DefaultFont, FontStyle.Bold);
        _title.Padding = new Padding(0, 0, 0, 6);
        _title.UseMnemonic = false;

        // Enter is not taken by the text (AcceptsReturn is off), so it reaches the default button.
        _text.Multiline = true;
        _text.ReadOnly = true;
        _text.WordWrap = true;
        _text.ScrollBars = ScrollBars.Vertical;
        _text.AcceptsTab = false;
        _text.BackColor = SystemColors.Window;
        _text.ForeColor = SystemColors.WindowText;
        _text.Dock = DockStyle.Fill;
        _text.TabIndex = 2;
        _text.KeyDown += EditKeys.Handle;
        _text.KeyDown += OnTextKeyDown;

        _inputLabel.AutoSize = true;
        _inputLabel.Location = new Point(0, 6);
        _input.Location = new Point(0, 2);
        _inputRow.Dock = DockStyle.Top;
        _inputRow.Height = 32;
        _inputRow.TabIndex = 1;
        _inputRow.Controls.Add(_inputLabel);
        _inputRow.Controls.Add(_input);
        _inputRow.SizeChanged += (_, _) => LayoutInput();
        _input.KeyDown += OnInputKeyDown;

        // The New session controls (FR-8.6), top to bottom under the text: the Choose folder button, the presets
        // in a named group (arrow keys move between them, Tab lands on the selected one), the Custom field.
        _session.Dock = DockStyle.Fill;
        _session.FlowDirection = FlowDirection.TopDown;
        _session.WrapContents = false;
        _session.AutoScroll = true;
        _session.Padding = new Padding(0, 6, 0, 0);
        _session.Visible = false;
        _session.TabIndex = 3;
        _session.SizeChanged += (_, _) => LayoutSession();

        _chooseFolder.Text = "Choose &folder...";
        _chooseFolder.AutoSize = true;
        _chooseFolder.Height = 32;
        _chooseFolder.Margin = new Padding(0, 0, 0, 8);
        _chooseFolder.UseVisualStyleBackColor = true;
        _chooseFolder.TabIndex = 0;
        _chooseFolder.Click += (_, _) => _sessionSpec?.ChooseFolder();

        _presetGroup.Text = "How Claude starts";
        _presetGroup.AutoSize = true;
        _presetGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _presetGroup.Margin = new Padding(0, 0, 0, 8);
        _presetGroup.TabIndex = 1;
        _presetList.FlowDirection = FlowDirection.TopDown;
        _presetList.WrapContents = false;
        _presetList.AutoSize = true;
        _presetList.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _presetList.Location = new Point(8, 20);
        _presetList.Padding = new Padding(0, 0, 8, 4);
        _presetGroup.Controls.Add(_presetList);

        _customRow.FlowDirection = FlowDirection.TopDown;
        _customRow.WrapContents = false;
        _customRow.AutoSize = true;
        _customRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _customRow.Margin = new Padding(0);
        _customRow.TabIndex = 2;
        _customLabel.AutoSize = true;
        _customLabel.Text = "Arguments for Claude Code, one or more on each line. Enter starts the session, Shift+Enter starts a new line.";
        _customLabel.Margin = new Padding(0, 0, 0, 4);
        _custom.Multiline = true;
        _custom.AcceptsReturn = true;
        _custom.WordWrap = true;
        _custom.ScrollBars = ScrollBars.Vertical;
        _custom.AccessibleName = "Custom arguments";
        _custom.Margin = new Padding(0);
        _custom.KeyDown += OnCustomKeyDown;
        _custom.KeyDown += EditKeys.Handle;
        _customRow.Controls.Add(_customLabel);
        _customRow.Controls.Add(_custom);
        _session.Controls.AddRange([_chooseFolder, _presetGroup, _customRow]);

        _buttons.Dock = DockStyle.Bottom;
        _buttons.FlowDirection = FlowDirection.LeftToRight;
        _buttons.WrapContents = true;
        _buttons.AutoSize = true;
        _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttons.Padding = new Padding(0, 6, 0, 0);
        _buttons.TabIndex = 5;

        // A download's progress bar (Notice.Progress), above the buttons; hidden otherwise.
        _progress.Dock = DockStyle.Bottom;
        _progress.Height = 22;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.AccessibleName = "Progress";
        _progress.Visible = false;

        // Docked controls are laid out from the last in the collection to the first: the buttons take the bottom,
        // the progress bar the strip above them, the title the top, the field the strip under the title, then the
        // text takes what is left, or, in a New session notice, a strip of a few lines with the session controls
        // filling the rest under it.
        Controls.AddRange([_session, _text, _inputRow, _title, _progress, _buttons]);
    }

    /// <summary>A button was pressed. The window closes the notice (unless the choice stays open) and runs its action.</summary>
    public event Action<OverlayChoice>? Chosen;

    /// <summary>
    /// The window's keys for moving through the text by Markdown structure (AX-7.2): one pair per kind from the window's
    /// own table, so that a notice follows the window's convention (AxClaude's letters, AxDown's Ctrl+letters). Empty
    /// until the window sets it.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TextJump> Jumps { get; set; } = [];

    /// <summary>The button Enter presses: the window's AcceptButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? DefaultButton { get; private set; }

    /// <summary>The button Escape presses: the window's CancelButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? CancelButton { get; private set; }

    /// <summary>The text field's current text.</summary>
    public string InputText => _input.Text;

    /// <summary>Where the caret is in the text field.</summary>
    public int InputCaret => _input.SelectionStart;

    /// <summary>The text field's text replaced, the caret after it: a completion the window chose.</summary>
    public void SetInputText(string text)
    {
        _input.Text = text;
        _input.Select(text.Length, 0);
    }

    /// <summary>The folder shown in the New session notice; null while none is chosen.</summary>
    public string? SessionFolder => _sessionFolder;

    /// <summary>The preset selected in the New session notice; null when Custom is selected.</summary>
    public OverlayPreset? SelectedPreset
    {
        get
        {
            var index = _presets.FindIndex(radio => radio.Checked);
            return _sessionSpec is { } session && index >= 0 && index < session.Presets.Count ? session.Presets[index] : null;
        }
    }

    /// <summary>The text of the Custom field in the New session notice.</summary>
    public string CustomArguments => _custom.Text;

    /// <summary>The font of the text, kept in step with the conversation's by the window.</summary>
    public void SetTextFont(Font font)
    {
        _text.Font = font;
        if (_sessionSpec is not null)
        {
            _text.Height = TextStripHeight();
        }
    }

    /// <summary>
    /// Fills the panel with a notice, whose text the window has completed with the key line. The text box carries the
    /// title as its accessible name, so a screen reader hears the title and then the first line when the focus lands
    /// on it. A notice the user did not open holds its answering keys for a moment.
    /// </summary>
    public void Populate(Notice notice)
    {
        var (title, text, choices) = (notice.Title, notice.Text, notice.Choices);
        var (input, session) = (notice.Input, notice.Session);
        _holdUntil = notice.Unprompted ? Environment.TickCount64 + HoldMs : 0;
        _title.Text = title;
        _text.AccessibleName = title;
        _body = WithKeyLine(text, choices);
        _inputSpec = input;
        _sessionSpec = session;
        _session.Visible = session is not null;
        if (session is not null)
        {
            // The captions are the window's: AxClaude's ways to start Claude, AxTerminal's shells.
            _presetGroup.Text = session.PresetsCaption;
            _custom.AccessibleName = session.CustomLabel;
            _customLabel.Text = session.CustomHelp;
            _text.Dock = DockStyle.Top;
            _text.Height = TextStripHeight();
            FillSession(session);
        }
        else
        {
            _text.Dock = DockStyle.Fill;
        }

        SetText();
        _progress.Visible = notice.Progress;
        if (notice.Progress)
        {
            _progress.Style = ProgressBarStyle.Marquee;
        }

        _inputRow.Visible = input is not null;
        if (input is not null)
        {
            _inputLabel.Text = input.Label;
            _input.AccessibleName = input.AccessibleName;
            _input.Text = input.Initial;
            _input.SelectAll();
            LayoutInput();
        }
        else
        {
            // A notice without the field must not see what was last typed in it (a Find text).
            _input.Text = string.Empty;
        }

        var old = _buttons.Controls.Cast<Control>().ToList();
        _buttons.Controls.Clear();
        foreach (var control in old)
        {
            control.Dispose();
        }

        DefaultButton = null;
        CancelButton = null;
        foreach (var choice in choices)
        {
            var button = new Button
            {
                Text = choice.Text,
                AutoSize = true,
                Height = 32,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = true,
            };
            button.Click += (_, _) => Choose(choice);
            _buttons.Controls.Add(button);
            if (choice.IsDefault)
            {
                DefaultButton = button;
            }

            if (choice.IsCancel)
            {
                CancelButton = button;
            }
        }
    }

    /// <summary>The progress bar of a <see cref="Notice.Progress"/> notice: 0 to 100, or a marquee while the total is unknown (below 0).</summary>
    public void SetProgress(int percent)
    {
        if (percent < 0)
        {
            _progress.Style = ProgressBarStyle.Marquee;
            return;
        }

        _progress.Style = ProgressBarStyle.Blocks;
        _progress.Value = Math.Clamp(percent, 0, 100);
    }

    /// <summary>The folder chosen through the Choose folder button: the first line of the text changes and is spoken.</summary>
    public void SetSessionFolder(string folder)
    {
        _sessionFolder = folder;
        SetText();
        Announce("Folder: " + folder, true);
    }

    /// <summary>Puts the focus where reading starts: in the text field when there is one, otherwise at the top of the text.</summary>
    public void FocusStart()
    {
        if (_inputSpec is not null)
        {
            _input.Select();
            return;
        }

        _text.Select(0, 0);
        _text.Select();
    }

    /// <summary>Speaks through a UI Automation notification raised on the notice, which holds the focus while it shows.</summary>
    public bool Announce(string text, bool interrupt)
    {
        Control source = _inputSpec is not null ? _input : _text;
        return Announcer.Raise(source, text, interrupt);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Keys typed for the message field just before a notice the user did not open must not answer it.
        if (Environment.TickCount64 < _holdUntil && keyData is Keys.Enter or Keys.Space)
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void Choose(OverlayChoice choice)
    {
        if (choice.IsDefault && _inputSpec is { Required: true } spec && _input.Text.Length == 0)
        {
            // Nothing to act on yet (Find with an empty field): say so and stay.
            Announce(spec.EmptyMessage, true);
            _input.Select();
            return;
        }

        if (choice.IsDefault && _sessionSpec is not null && _sessionFolder is null)
        {
            Announce(ChooseFolderFirst, true);
            _chooseFolder.Select();
            return;
        }

        Chosen?.Invoke(choice);
    }

    /// <summary>
    /// The last line of every notice, made from its buttons so that no notice words its keys itself: "Enter: Close
    /// anyway. Escape: Keep working.", or "Enter or Escape: Close." for a single button; the whole text of a notice
    /// with no words of its own (AxDown's save question), so that there is always text to land on.
    /// </summary>
    private static string WithKeyLine(string text, IReadOnlyList<OverlayChoice> choices)
    {
        var enter = choices.FirstOrDefault(choice => choice.IsDefault);
        var escape = choices.FirstOrDefault(choice => choice.IsCancel);
        // The button's text without its mnemonic marker or a trailing ellipsis.
        static string Label(OverlayChoice choice) => choice.Text.Replace("&", string.Empty).TrimEnd('.');
        var keys = enter is not null && ReferenceEquals(enter, escape) ? $"Enter or Escape: {Label(enter)}."
            : string.Join(" ", new[] { enter is null ? null : $"Enter: {Label(enter)}.", escape is null ? null : $"Escape: {Label(escape)}." }.OfType<string>());
        return keys.Length == 0 ? text : text.Length == 0 ? keys : text.TrimEnd('\n') + "\n" + keys;
    }

    private void SetText()
    {
        var body = _body.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _text.Text = _sessionSpec is null
            ? body
            : "Folder: " + (_sessionFolder ?? NoFolder) + (body.Length > 0 ? "\r\n" + body : string.Empty);
        _text.Visible = _text.Text.Length > 0;
    }

    private void FillSession(OverlaySession session)
    {
        _sessionFolder = session.Folder;
        foreach (var old in _presets)
        {
            _presetList.Controls.Remove(old);
            old.Dispose();
        }

        _presets.Clear();
        for (var i = 0; i <= session.Presets.Count; i++)
        {
            var custom = i == session.Presets.Count;
            var radio = new RadioButton
            {
                Text = custom ? "Custom" : session.Presets[i].Text,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2),
                TabIndex = i,
                UseVisualStyleBackColor = true,
            };
            if (custom)
            {
                radio.CheckedChanged += (_, _) => _customRow.Visible = radio.Checked;
            }

            _presets.Add(radio);
            _presetList.Controls.Add(radio);
        }

        _custom.Text = session.CustomArguments.Replace("\r\n", "\n").Replace("\n", "\r\n");
        var selected = Math.Clamp(session.Selected, 0, session.Presets.Count);
        _customRow.Visible = selected == session.Presets.Count;
        _presets[selected].Checked = true;
        LayoutSession();
    }

    /// <summary>A navigation key in the text (AX-7.2): the shared jump with the window's pairs.</summary>
    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (TextJumps.Handle(_text, Jumps, e.KeyData, Announce))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    /// <summary>A key in the field that the window asked for (<see cref="OverlayInput.KeyHook"/>), such as AxFile's path completion.</summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (_inputSpec?.KeyHook is { } hook && hook(e.KeyData))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private void OnCustomKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter starts the session and Shift+Enter is a new line, as in the message field.
        if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            DefaultButton?.PerformClick();
        }
    }

    private int TextStripHeight() => _text.Font.Height * 4 + 10;

    private void LayoutInput()
    {
        _input.Left = _inputLabel.Right + 6;
        _input.Width = Math.Max(100, _inputRow.ClientSize.Width - _input.Left);
    }

    private void LayoutSession()
    {
        var width = Math.Max(200, _session.ClientSize.Width - 24);
        _customLabel.MaximumSize = new Size(width, 0);
        _custom.Width = width;
        _custom.Height = _custom.Font.Height * 4 + 8;
    }
}
