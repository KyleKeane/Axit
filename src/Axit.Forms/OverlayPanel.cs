
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
/// first and takes it by returning true (axFile's path completion on Down, Up and Right). <paramref name="Changed"/> is
/// called on each change of the text, and <paramref name="ListName"/>, when given, shows a list under the field that
/// never takes the focus, filled by the window with <see cref="OverlayPanel.SetMatches"/> (the command palette, AX-10).
/// <paramref name="Hidden"/> hides what is typed, as a password field does (axPass's master password, docs/axpass/SPEC.md
/// PW-4.2): the screen shows dots and NVDA reads the field as protected.
/// </summary>
public sealed record OverlayInput(string Label, string AccessibleName, string Initial, string EmptyMessage, bool Required = true, Func<Keys, bool>? KeyHook = null, Action? Changed = null, string? ListName = null, bool Hidden = false);

/// <summary>
/// What a notice says and offers, and nothing about how it is shown (D23): the title, the text, the buttons, at most
/// one of the text field and a control of the window's own, and whether the user opened it. Every notice goes
/// through the window base's <c>AxitForm.ShowNotice</c>, which adds what all notices share: the key line made from
/// the buttons, the chime and the hold of a notice the user did not open, the focus, and the blocked window.
/// </summary>
public sealed record Notice(string Title, string Text, IReadOnlyList<OverlayChoice> Choices)
{
    public OverlayInput? Input { get; init; }

    /// <summary>
    /// A control of the window's own under the text, which the window makes, reads and keeps (the New session
    /// controls, <see cref="SessionControls"/>); the panel shows it in the text's place below a strip of a few lines.
    /// </summary>
    public Control? Custom { get; init; }

    /// <summary>Asked before the default button acts, when given: false keeps the notice open, the window having said why (no folder chosen yet).</summary>
    public Func<bool>? Accepts { get; init; }

    /// <summary>A first line of the text that the window changes while the notice shows (the folder of New session, <see cref="OverlayPanel.SetHeadLine"/>); null for none.</summary>
    public string? HeadLine { get; init; }

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
/// text field or an optional control of the window's own, and a row of buttons; it shows a <see cref="Notice"/>,
/// which says what, and the window decides the rest. The window hides the controls underneath, blocks its own
/// shortcuts and the menu, and routes Enter and Escape to the default and cancel buttons through its AcceptButton
/// and CancelButton; Tab moves between the text, the field or the window's control, and the buttons.
/// </summary>
public sealed class OverlayPanel : Panel
{
    /// <summary>A notice the user did not open ignores the keys that answer (Enter, Space) for a moment: it can open while the user is typing a message, and the next keys were meant for the message field.</summary>
    private readonly KeyHold _hold = new();

    private readonly Label _title = new();
    private readonly TextBox _text = new();
    private readonly Panel _inputRow = new();
    private readonly Label _inputLabel = new();
    private readonly TextBox _input = new();
    private readonly FlowLayoutPanel _buttons = new();
    private readonly ProgressBar _progress = new();
    private readonly ListBox _matches = new();
    private OverlayInput? _inputSpec;
    private Control? _custom;
    private string? _headLine;
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
        _input.TextChanged += (_, _) => _inputSpec?.Changed?.Invoke();

        // The matches under the field (OverlayInput.ListName): for the eyes and NVDA's object navigation, never the
        // focus, as axStart's list (AS-D2); the field's keys move the selection.
        _matches.Dock = DockStyle.Fill;
        _matches.IntegralHeight = false;
        _matches.TabStop = false;
        _matches.Visible = false;
        _matches.GotFocus += (_, _) => _input.Focus();

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
        // text takes what is left, or, with a control of the window's own, a strip of a few lines with that control
        // filling the rest under it (it is put first in the collection when a notice brings one).
        Controls.AddRange([_matches, _text, _inputRow, _title, _progress, _buttons]);
    }

    /// <summary>A button was pressed. The window closes the notice (unless the choice stays open) and runs its action.</summary>
    public event Action<OverlayChoice>? Chosen;

    /// <summary>
    /// The keys for moving through the text by Markdown structure (AX-7.2): the heading letters every window's notices
    /// take (<see cref="TextJump.HeadingLetters"/>) unless the window sets pairs of its own (an editor adds its
    /// Ctrl+letter pairs, so that its convention holds in its notices too).
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TextJump> Jumps { get; set; } = TextJump.HeadingLetters;

    /// <summary>The notice shown, for a flow that shows it again (find in its text, AX-7.2); null before the first.</summary>
    public Notice? Shown { get; private set; }

    /// <summary>The notice's read-only text, for the window's find in it (AX-7.2): the caret kept and the match selected there.</summary>
    public TextBox Body => _text;

    /// <summary>Whether the text can be searched: a notice with text and neither a field nor a control of the window's own, since the text holds the focus then.</summary>
    public bool HasSearchableText => _text.Visible && _inputSpec is null && _custom is null;

    /// <summary>The button Enter presses: the window's AcceptButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? DefaultButton { get; private set; }

    /// <summary>The button Escape presses: the window's CancelButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? CancelButton { get; private set; }

    /// <summary>The text field's current text.</summary>
    public string InputText => _input.Text;

    /// <summary>
    /// A password typed into a notice's hidden field goes with the notice, whichever way it closed, Cancel and Escape
    /// included (security audit S-17): the notice's action has read it by then, and the field held it until the next
    /// notice before.
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible && _input.UseSystemPasswordChar)
        {
            _input.Text = string.Empty;
        }

        base.OnVisibleChanged(e);
    }

    /// <summary>Where the caret is in the text field.</summary>
    public int InputCaret => _input.SelectionStart;

    /// <summary>The text field's text replaced, the caret after it: a completion the window chose.</summary>
    public void SetInputText(string text)
    {
        _input.Text = text;
        _input.Select(text.Length, 0);
    }

    /// <summary>The list under the field (OverlayInput.ListName) filled anew, named "Commands, 3", the first line selected.</summary>
    public void SetMatches(IReadOnlyList<string> lines, string name)
    {
        _matches.BeginUpdate();
        _matches.Items.Clear();
        _matches.Items.AddRange(lines.Select(line => (object)line).ToArray());
        _matches.EndUpdate();
        _matches.AccessibleName = name;
        _matches.SelectedIndex = lines.Count > 0 ? 0 : -1;
    }

    /// <summary>The selected line of the list under the field.</summary>
    public int MatchIndex => _matches.SelectedIndex;

    /// <summary>Selects a line of the list under the field.</summary>
    public void SelectMatch(int index) => _matches.SelectedIndex = index;

    /// <summary>The lines the list shows at once, for Page Up and Page Down.</summary>
    public int MatchPage => Math.Max(1, _matches.ClientSize.Height / Math.Max(1, _matches.ItemHeight));

    /// <summary>The first line of the text changed while the notice shows (<see cref="Notice.HeadLine"/>): the folder of New session.</summary>
    public void SetHeadLine(string? line)
    {
        _headLine = line;
        SetText();
    }

    /// <summary>The default button pressed, as Enter presses it: a control of the window's own asks for it (Enter in the session controls' free field).</summary>
    public void PressDefault() => DefaultButton?.PerformClick();

    /// <summary>The font of the text, kept in step with the conversation's by the window.</summary>
    public void SetTextFont(Font font)
    {
        _text.Font = font;
        _matches.Font = font;
        if (_custom is not null)
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
        Shown = notice;
        var (title, text, choices) = (notice.Title, notice.Text, notice.Choices);
        var input = notice.Input;
        if (notice.Unprompted)
        {
            _hold.Start();
        }
        else
        {
            _hold.Clear();
        }

        _title.Text = title;
        _text.AccessibleName = title;
        _body = WithKeyLine(text, choices);
        _headLine = notice.HeadLine;
        // The field's spec goes in after its text is set, so that Changed does not fire for the filling (the palette
        // filled its list twice), nor the previous notice's Changed for the new text.
        _inputSpec = null;
        ShowCustom(notice.Custom);
        if (_custom is not null)
        {
            // The text in a strip of a few lines, the window's control filling the rest under it.
            _text.Dock = DockStyle.Top;
            _text.Height = TextStripHeight();
        }
        else if (input?.ListName is not null)
        {
            // The key line in a strip under the field, the list filling the rest.
            _text.Dock = DockStyle.Top;
            _text.Height = _text.Font.Height * 2 + 10;
        }
        else
        {
            _text.Dock = DockStyle.Fill;
        }

        _matches.Visible = input?.ListName is not null;
        _matches.AccessibleName = input?.ListName ?? string.Empty;
        _matches.Items.Clear();
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
            _input.UseSystemPasswordChar = input.Hidden;
            _input.Text = input.Initial;
            _input.SelectAll();
            _inputSpec = input;
            LayoutInput();
        }
        else
        {
            // A notice without the field must not see what was last typed in it (a Find text, a password).
            _input.Text = string.Empty;
            _input.UseSystemPasswordChar = false;
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
        if (_hold.Holding && keyData is Keys.Enter or Keys.Space)
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>The window's control shown under the text in place of the one before, which the window keeps (it reads the choice from it); none for a plain notice.</summary>
    private void ShowCustom(Control? custom)
    {
        if (ReferenceEquals(_custom, custom))
        {
            return;
        }

        if (_custom is not null)
        {
            Controls.Remove(_custom);
        }

        _custom = custom;
        if (custom is not null)
        {
            custom.Dock = DockStyle.Fill;
            Controls.Add(custom);
            // First in the collection: laid out last, so it takes what the others leave.
            Controls.SetChildIndex(custom, 0);
        }
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

        if (choice.IsDefault && Shown?.Accepts is { } accepts && !accepts())
        {
            // The window's control is not ready (no folder chosen yet): it said so and took the focus.
            return;
        }

        Chosen?.Invoke(choice);
    }

    /// <summary>
    /// The last line of every notice, made from its buttons so that no notice words its keys itself: "Enter: Close
    /// anyway. Escape: Keep working.", or "Enter or Escape: Close." for a single button; the whole text of a notice
    /// with no words of its own (axDown's save question), so that there is always text to land on.
    /// </summary>
    private static string WithKeyLine(string text, IReadOnlyList<OverlayChoice> choices)
    {
        var enter = choices.FirstOrDefault(choice => choice.IsDefault);
        var escape = choices.FirstOrDefault(choice => choice.IsCancel);
        // The button's text as spoken: without its mnemonic marker or a trailing ellipsis.
        static string Label(OverlayChoice choice) => MenuText.Plain(choice.Text);
        var keys = enter is not null && ReferenceEquals(enter, escape) ? $"Enter or Escape: {Label(enter)}."
            : string.Join(" ", new[] { enter is null ? null : $"Enter: {Label(enter)}.", escape is null ? null : $"Escape: {Label(escape)}." }.OfType<string>());
        return keys.Length == 0 ? text : text.Length == 0 ? keys : text.TrimEnd('\n') + "\n" + keys;
    }

    private void SetText()
    {
        var body = _body.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _text.Text = _headLine is null
            ? body
            : _headLine + (body.Length > 0 ? "\r\n" + body : string.Empty);
        _text.Visible = _text.Text.Length > 0;
    }

    /// <summary>A navigation key in the text (AX-7.2): the shared jump with the window's pairs.</summary>
    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (TextJumps.Handle(_text, Jumps, e.KeyData, Announce))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    /// <summary>A key in the field that the window asked for (<see cref="OverlayInput.KeyHook"/>), such as axFile's path completion.</summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (_inputSpec?.KeyHook is { } hook && hook(e.KeyData))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private int TextStripHeight() => _text.Font.Height * 4 + 10;

    private void LayoutInput()
    {
        _input.Left = _inputLabel.Right + 6;
        _input.Width = Math.Max(100, _inputRow.ClientSize.Width - _input.Left);
    }
}
