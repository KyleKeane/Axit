namespace Axit.Forms;

/// <summary>A recommended way to start the session (AxClaude FR-8.6, AxTerminal AT-1.6): the radio button's text and the arguments it stands for.</summary>
public sealed record SessionPreset(string Text, IReadOnlyList<string> Arguments);

/// <summary>
/// The New session controls of AxClaude and AxTerminal (FR-8.6, AT-1.6), a control of the window's own that a
/// <see cref="Notice"/> carries (<see cref="Notice.Custom"/>), under the notice's text: the Choose folder button,
/// the presets as radio buttons in a named group (the arrow keys move between them, Tab lands on the selected one)
/// with Custom last, and the free field under Custom, where Enter presses the notice's default button and
/// Shift+Enter is a new line. The window makes it with its own wording, reads the choice from it when the notice is
/// answered, and gives it the folder picker, the announcer, the notice's head line and its default button. Out of
/// <see cref="OverlayPanel"/> since 2026-10-23 (docs/axit/review-2026-10-23.md 5.6), where it sat because the panel
/// moved whole from AxClaude.
/// </summary>
public sealed class SessionControls : FlowLayoutPanel
{
    private const string NoFolder = "(none chosen yet)";

    private readonly Button _chooseFolder = new();
    private readonly GroupBox _presetGroup = new();
    private readonly FlowLayoutPanel _presetList = new();
    private readonly FlowLayoutPanel _customRow = new();
    private readonly Label _customLabel = new();
    private readonly TextBox _custom = new();
    private readonly List<RadioButton> _presets = [];
    private readonly IReadOnlyList<SessionPreset> _specs;
    private readonly Func<string?, string?> _pickFolder;
    private readonly Action<string> _say;
    private readonly Action<string> _headLine;

    /// <param name="folder">The folder shown at first; null when none is chosen yet.</param>
    /// <param name="presets">The recommended ways, in order; Custom follows them.</param>
    /// <param name="selected">The preset selected at first; the number of presets selects Custom.</param>
    /// <param name="customArguments">The free field's text at first.</param>
    /// <param name="presetsCaption">The group's caption: "How Claude starts", "Shell".</param>
    /// <param name="customLabel">The free field's accessible name: "Custom arguments", "Command line".</param>
    /// <param name="customHelp">The help line above the free field.</param>
    /// <param name="pickFolder">The window's folder picker, given the folder to start on; null when it was cancelled.</param>
    /// <param name="say">The window's announcer, interrupting.</param>
    /// <param name="headLine">Sets the first line of the notice's text, the folder line, when the folder changes.</param>
    /// <param name="accept">Presses the notice's default button (Enter in the free field).</param>
    public SessionControls(string? folder, IReadOnlyList<SessionPreset> presets, int selected, string customArguments,
        string presetsCaption, string customLabel, string customHelp,
        Func<string?, string?> pickFolder, Action<string> say, Action<string> headLine, Action accept)
    {
        Folder = folder;
        _specs = presets;
        _pickFolder = pickFolder;
        _say = say;
        _headLine = headLine;

        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = true;
        Padding = new Padding(0, 6, 0, 0);
        TabIndex = 3;
        SizeChanged += (_, _) => LayoutRows();

        _chooseFolder.Text = "Choose &folder...";
        _chooseFolder.AutoSize = true;
        _chooseFolder.Height = 32;
        _chooseFolder.Margin = new Padding(0, 0, 0, 8);
        _chooseFolder.UseVisualStyleBackColor = true;
        _chooseFolder.TabIndex = 0;
        _chooseFolder.Click += (_, _) => ChooseFolder();

        _presetGroup.Text = presetsCaption;
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
        for (var i = 0; i <= presets.Count; i++)
        {
            var custom = i == presets.Count;
            var radio = new RadioButton
            {
                Text = custom ? "Custom" : presets[i].Text,
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

        _customRow.FlowDirection = FlowDirection.TopDown;
        _customRow.WrapContents = false;
        _customRow.AutoSize = true;
        _customRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _customRow.Margin = new Padding(0);
        _customRow.TabIndex = 2;
        _customLabel.AutoSize = true;
        _customLabel.Text = customHelp;
        _customLabel.Margin = new Padding(0, 0, 0, 4);
        _custom.Multiline = true;
        _custom.AcceptsReturn = true;
        _custom.WordWrap = true;
        _custom.ScrollBars = ScrollBars.Vertical;
        _custom.AccessibleName = customLabel;
        _custom.Margin = new Padding(0);
        _custom.Text = customArguments.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _custom.KeyDown += (_, e) =>
        {
            // Enter starts the session and Shift+Enter is a new line, as in the message field.
            if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None)
            {
                e.Handled = e.SuppressKeyPress = true;
                accept();
            }
        };
        _custom.KeyDown += EditKeys.Handle;
        _customRow.Controls.Add(_customLabel);
        _customRow.Controls.Add(_custom);
        Controls.AddRange([_chooseFolder, _presetGroup, _customRow]);

        var chosen = Math.Clamp(selected, 0, presets.Count);
        _customRow.Visible = chosen == presets.Count;
        _presets[chosen].Checked = true;
        LayoutRows();
    }

    /// <summary>The folder shown; null while none is chosen.</summary>
    public string? Folder { get; private set; }

    /// <summary>The first line of the notice's text: "Folder: C:\path", or "(none chosen yet)".</summary>
    public string FolderLine => "Folder: " + (Folder ?? NoFolder);

    /// <summary>The preset selected; null when Custom is.</summary>
    public SessionPreset? SelectedPreset
    {
        get
        {
            var index = _presets.FindIndex(radio => radio.Checked);
            return index >= 0 && index < _specs.Count ? _specs[index] : null;
        }
    }

    /// <summary>The free field's text.</summary>
    public string CustomArguments => _custom.Text;

    /// <summary>Whether the notice's default button may act (<see cref="Notice.Accepts"/>): not before a folder is chosen, which is said, with the focus on the button.</summary>
    public bool Ready()
    {
        if (Folder is not null)
        {
            return true;
        }

        _say("Choose a folder first");
        _chooseFolder.Select();
        return false;
    }

    /// <summary>The Choose folder button: the picker, then the folder line of the notice changes and is said; the notice stays open.</summary>
    private void ChooseFolder()
    {
        if (_pickFolder(Folder) is not { } folder)
        {
            return;
        }

        Folder = folder;
        _headLine(FolderLine);
        _say(FolderLine);
    }

    private void LayoutRows()
    {
        var width = Math.Max(200, ClientSize.Width - 24);
        _customLabel.MaximumSize = new Size(width, 0);
        _custom.Width = width;
        _custom.Height = _custom.Font.Height * 4 + 8;
    }
}
