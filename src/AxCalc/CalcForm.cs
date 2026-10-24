using AxCalc.Core;
using Axit.Core;
using Axit.Forms;

namespace AxCalc;

/// <summary>
/// axCalc's window (SPEC.md AC-2): a menu bar, the history as a list, the expression field under it, and a status bar
/// with the last answer. Enter in the field answers in one announcement; the history keeps every answer to read back,
/// copy and recall. The evaluator is <see cref="Calculator"/> in the Core; the notices, the fonts, the window's place,
/// find and the update flow are the bundle's window base (<see cref="AxitForm"/>).
/// </summary>
public sealed class CalcForm : AxitForm
{
    private readonly CalcSettings _settings;
    private readonly Calculator _calculator = new();
    private readonly List<CalcEntry> _entries = [];
    private readonly MenuStrip _menu = new();
    private readonly ListBox _history = new();
    private readonly TextBox _field = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _answerLabel;
    private readonly ToolStripStatusLabel _unitLabel;
    private readonly ToolStripMenuItem _degreesItem = new("&Degrees");
    private readonly ToolStripMenuItem _radiansItem = new("&Radians");

    public CalcForm(CalcSettings settings, string? settingsError)
        : base(AxCalcApp.Name, settings, CalcSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        Text = "Calculator - " + AxCalcApp.Name;
        _calculator.Degrees = settings.Degrees;
        foreach (var (name, value) in settings.Variables)
        {
            _calculator.SetVariable(name, value);
        }

        _entries.AddRange(settings.History);
        _calculator.Answer = _entries.Count > 0 ? _entries[^1].Value : null;

        // ---- menus (AC-7.1); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        var calculator = new ToolStripMenuItem("&Calculator");
        calculator.DropDownItems.Add(Item("&Answer", Evaluate, AxCalcKeys.Act));
        calculator.DropDownItems.Add(Item("&Copy answer", CopyAnswer, AxCalcKeys.CopyAnswer));
        var copyIn = new ToolStripMenuItem("Copy answer &in");
        copyIn.DropDownItems.Add(new ToolStripMenuItem("&Hexadecimal", null, (_, _) => CopyCode(16)));
        copyIn.DropDownItems.Add(new ToolStripMenuItem("&Binary", null, (_, _) => CopyCode(2)));
        copyIn.DropDownItems.Add(new ToolStripMenuItem("&Octal", null, (_, _) => CopyCode(8)));
        calculator.DropDownItems.Add(copyIn);
        calculator.DropDownItems.Add(Item("Answer in other &bases", SayBases, AxCalcKeys.OtherBases));
        calculator.DropDownItems.Add(Item("Say the &last answer", SayLastAnswer, AxCalcKeys.LastAnswer));
        calculator.DropDownItems.Add(new ToolStripSeparator());
        calculator.DropDownItems.Add(new ToolStripMenuItem("&Variables", null, (_, _) => ShowVariables()));
        calculator.DropDownItems.Add(new ToolStripMenuItem("Clear va&riables...", null, (_, _) => ClearVariables()));
        calculator.DropDownItems.Add(new ToolStripSeparator());
        calculator.DropDownItems.Add(Item("&Save history...", SaveHistory, BundleKeys.Save));
        calculator.DropDownItems.Add(Item("Clear &history...", ClearHistory, AxCalcKeys.ClearHistory));
        calculator.DropDownItems.Add(new ToolStripSeparator());
        calculator.DropDownItems.Add(CreateExitItem());

        // Options, as the other windows' (AX-6): the window's own submenus and items first, then Text, General, Developer.
        var options = new ToolStripMenuItem("&Options");
        var angles = new ToolStripMenuItem("&Angles");
        _degreesItem.Click += (_, _) => SetDegrees(true);
        _radiansItem.Click += (_, _) => SetDegrees(false);
        angles.DropDownItems.AddRange([_degreesItem, _radiansItem]);
        options.DropDownItems.Add(angles);
        options.DropDownItems.Add(Toggle("&Keep history between runs", settings.KeepHistory, v =>
        {
            // AC-9.1: off, the file holds no history from the save that follows.
            _settings.KeepHistory = v;
            _settings.Keep(_entries, _calculator.Variables);
        }));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Calculator &resources",
            [("&Floating point explained", "https://floating-point-gui.de")]);

        // The bundle's Axit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([calculator, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the history and the field (AC-2.1) ----
        _history.IntegralHeight = false;
        _history.Dock = DockStyle.Fill;
        _history.TabIndex = 0;
        _history.Items.AddRange(_entries.Select(entry => (object)entry.Label).ToArray());
        if (_history.Items.Count > 0)
        {
            // AC-2.4: on the last line, so that the first Shift+Tab reads the latest answer.
            _history.SelectedIndex = _history.Items.Count - 1;
        }

        _field.Dock = DockStyle.Bottom;
        _field.TabIndex = 1;
        _field.AccessibleName = "Expression";

        (_answerLabel, _unitLabel) = StatusLayout.Pair(_status);

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the field the
        // line above it, and the history (or the notice over both) what is left.
        Controls.AddRange([Overlay, _history, _field, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        UpdateAngles();
        AfterChange();
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        _history.Visible = false;
        _field.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _history.Visible = true;
        _field.Visible = true;
    }

    protected override Control FocusTarget => _field;

    /// <summary>The announcements on the control with the focus, so that NVDA speaks them where the user is.</summary>
    protected override Control AnnounceSource => _history.Focused ? _history : _field;

    protected override void ApplyFont(Font font)
    {
        _history.Font = font;
        _field.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxCalc];

    protected override IEnumerable<string> DiagnosticsLines =>
        [$"History: {_entries.Count} entries, {_calculator.Variables.Count} variables, {(_calculator.Degrees ? "degrees" : "radians")}"];

    /// <summary>Ctrl+S, the bundle's save key: the history as a text file (AC-5.5).</summary>
    protected override void SaveCommand() => SaveHistory();

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _field.Select();
        }

        base.OnShown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AC-9.1: the history and the variables into the settings, which the base then writes.
        _settings.Keep(_entries, _calculator.Variables);
        base.OnFormClosing(e);
    }

    // ---- keys (AC-7.2): every key comes from BundleKeys or AxCalcKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                Control other = _history.Focused ? _field : _history;
                other.Focus();
                return true;
            case BundleKeys.Find:
                ShowFindInList(_history, Labels);
                return true;
            case BundleKeys.FindNext:
                FindInList(_history, Labels, 1);
                return true;
            case BundleKeys.FindPrevious:
                FindInList(_history, Labels, -1);
                return true;
            case AxCalcKeys.CopyAnswer:
                CopyAnswer();
                return true;
            case AxCalcKeys.OtherBases:
                SayBases();
                return true;
            case AxCalcKeys.LastAnswer:
                SayLastAnswer();
                return true;
            case AxCalcKeys.ClearHistory:
                ClearHistory();
                return true;
        }

        if (_field.Focused)
        {
            switch (keyData)
            {
                case AxCalcKeys.Act:
                    Evaluate();
                    return true;
                case AxCalcKeys.Back:
                    ClearField();
                    return true;
            }
        }

        if (_history.Focused)
        {
            // Enter, Escape and Delete in the list (AC-5.2); returning true keeps the list from dinging.
            switch (keyData)
            {
                case AxCalcKeys.Act:
                    Recall();
                    return true;
                case AxCalcKeys.Back:
                    _field.Focus();
                    return true;
                case AxCalcKeys.Remove:
                    RemoveEntry();
                    return true;
            }
        }

        return false;
    }

    // ---- the answer (AC-3, AC-6) ----

    /// <summary>Enter in the field: the answer said and added to the history, the field emptied; an error said and the field kept to mend.</summary>
    private void Evaluate()
    {
        var text = _field.Text;
        if (text.Trim().Length == 0)
        {
            Announce("Type an expression", true);
            return;
        }

        var answer = _calculator.Evaluate(text);
        if (!answer.Ok)
        {
            Announce(answer.Spoken, true);
            return;
        }

        _entries.Add(CalcEntry.From(text, answer));
        _history.Items.Add(_entries[^1].Label);
        while (_entries.Count > CalcSettings.MaxHistory)
        {
            _entries.RemoveAt(0);
            _history.Items.RemoveAt(0);
        }

        _history.SelectedIndex = _history.Items.Count - 1;
        _field.Clear();
        AfterChange();
        Announce(answer.Spoken, true);
    }

    private void ClearField()
    {
        if (_field.TextLength == 0)
        {
            Announce("Empty", true);
            return;
        }

        _field.Clear();
        Announce("Cleared", true);
    }

    /// <summary>The value a command acts on: the selected entry's while the history has the focus, the last answer otherwise.</summary>
    private double? CurrentValue() =>
        _history.Focused && SelectedEntry() is { } entry ? entry.Value : _calculator.Answer;

    private CalcEntry? SelectedEntry() =>
        _history.SelectedIndex is var i && i >= 0 && i < _entries.Count ? _entries[i] : null;

    private void SayLastAnswer() =>
        Announce(_calculator.Answer is { } answer ? "Answer " + NumberText.Format(answer) : "No answer yet", true);

    /// <summary>Ctrl+I (AC-4.1): the answer in hexadecimal, binary and octal.</summary>
    private void SayBases()
    {
        if (CurrentValue() is not { } value)
        {
            Announce("No answer yet", true);
            return;
        }

        Announce(NumberText.Bases(value), true);
    }

    private void CopyAnswer()
    {
        if (CurrentValue() is not { } value)
        {
            Announce("No answer yet", true);
            return;
        }

        Copy(NumberText.Format(value));
    }

    /// <summary>Calculator → Copy answer in (AC-4.2): "0xFF", "0b11111111", "0o377"; why not, for an answer with no other bases.</summary>
    private void CopyCode(int radix)
    {
        if (CurrentValue() is not { } value)
        {
            Announce("No answer yet", true);
            return;
        }

        if (NumberText.Code(value, radix) is { } code)
        {
            Copy(code);
        }
        else
        {
            Announce(NumberText.Bases(value), true);
        }
    }

    private void Copy(string text)
    {
        try
        {
            Clipboard.SetText(text);
            Announce("Copied " + text, true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
        }
    }

    // ---- the history (AC-5) ----

    private IReadOnlyList<string> Labels() => _entries.Select(entry => entry.Label).ToList();

    /// <summary>What a history command says without an entry to act on.</summary>
    private string NothingSelected => _entries.Count == 0 ? "History, empty" : "No entry selected";

    /// <summary>Enter in the history: the entry's expression in the field, the caret at its end, the focus there.</summary>
    private void Recall()
    {
        if (SelectedEntry() is not { } entry)
        {
            Announce(NothingSelected, true);
            return;
        }

        _field.Text = entry.Expression;
        _field.Focus();
        _field.SelectionStart = _field.TextLength;
    }

    private void RemoveEntry()
    {
        var index = _history.SelectedIndex;
        if (index < 0 || index >= _entries.Count)
        {
            Announce(NothingSelected, true);
            return;
        }

        _entries.RemoveAt(index);
        _history.Items.RemoveAt(index);
        if (_history.Items.Count > 0)
        {
            _history.SelectedIndex = Math.Min(index, _history.Items.Count - 1);
        }

        AfterChange();
        Announce("Removed", true);
    }

    private void ClearHistory()
    {
        if (_entries.Count == 0)
        {
            Announce("History, empty", true);
            return;
        }

        var count = _entries.Count;
        ShowNotice(new Notice("Clear the history?", count == 1 ? "The entry cannot be brought back." : $"The {count} entries cannot be brought back.",
        [
            new OverlayChoice("C&lear", () =>
            {
                _entries.Clear();
                _history.Items.Clear();
                AfterChange();
                SaveState();
                Announce("History cleared", true);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>Ctrl+S (AC-5.5): one entry per line through the Windows save dialog.</summary>
    private void SaveHistory()
    {
        if (_entries.Count == 0)
        {
            Announce("History, empty", true);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Save the history",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = "calculations.txt",
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        var lines = Labels();
        Do(async () =>
        {
            try
            {
                await File.WriteAllLinesAsync(path, lines);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail($"{path} could not be written: {ex.Message}");
                return;
            }

            Announce("Saved " + Path.GetFileName(path), true);
        });
    }

    // ---- the variables (AC-3.6, AC-5.4) ----

    private void ShowVariables()
    {
        var variables = _calculator.Variables;
        var text = variables.Count == 0
            ? "No variables. Set one with name = value"
            : string.Join("\n", variables.Select(pair => $"{pair.Key} = {NumberText.Format(pair.Value)}"));
        ShowNotice(Notice.Plain("Variables", text));
    }

    private void ClearVariables()
    {
        var count = _calculator.Variables.Count;
        if (count == 0)
        {
            Announce("No variables", true);
            return;
        }

        ShowNotice(new Notice("Clear the variables?", count == 1 ? "The variable cannot be brought back." : $"The {count} variables cannot be brought back.",
        [
            new OverlayChoice("C&lear", () =>
            {
                _calculator.ClearVariables();
                AfterChange();
                SaveState();
                Announce("Variables cleared", true);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    // ---- settings and the status bar (AC-2.3, AC-9) ----

    private void SetDegrees(bool degrees)
    {
        _calculator.Degrees = degrees;
        _settings.Degrees = degrees;
        UpdateAngles();
        AfterChange();
        SaveSettings();
        Announce(degrees ? "Degrees" : "Radians", true);
    }

    private void UpdateAngles()
    {
        _degreesItem.Checked = _calculator.Degrees;
        _radiansItem.Checked = !_calculator.Degrees;
    }

    /// <summary>The history and the variables into the settings file now: after a clear, which should not come back after a crash.</summary>
    private void SaveState()
    {
        _settings.Keep(_entries, _calculator.Variables);
        SaveSettings();
    }

    /// <summary>The list's name and the status bar after the history, the variables or the unit changed.</summary>
    private void AfterChange()
    {
        var count = _entries.Count;
        _history.AccessibleName = count == 0 ? "History, empty" : $"History, {count} {(count == 1 ? "entry" : "entries")}";
        _answerLabel.Text = _calculator.Answer is { } answer ? "Answer " + NumberText.Format(answer) : "No answer yet";
        var variables = _calculator.Variables.Count;
        _unitLabel.Text = $"{(_calculator.Degrees ? "Degrees" : "Radians")}, {(variables == 0 ? "no variables" : variables == 1 ? "1 variable" : $"{variables} variables")}";
        StatusLayout.Fit(_status, _answerLabel, _unitLabel);
    }
}
