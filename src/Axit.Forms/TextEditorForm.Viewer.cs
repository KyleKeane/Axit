using System.ComponentModel;
using System.Diagnostics;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The viewer's own part of the editor shell (docs/axpreview/SPEC.md AP-D4): what the shell does only in its
/// read-only mode, axPreview. The app names here (Edit in axDown, Edit in axJSON, the <c>.json</c> rule, Play in
/// axMedia) are axPreview's rule of where a file goes on (AP-1.4, AP-7.3), written where the viewer lives rather than
/// in the viewer's data, since the shell makes the menu items. In its own file since 2026-10-23 (review 5.6); the
/// Details list (AP-6) and the renderers' pages (AP-7) since 2026-10-24.
/// </summary>
public sealed partial class TextEditorForm
{
    /// <summary>File → Edit in axDown or axJSON, Play in axMedia, or Open in Windows' program, named after the file's page; null in an editor.</summary>
    private ToolStripMenuItem? _editItem;

    /// <summary>File → Open with ▸, filled as it opens (AP-7.3); null in an editor.</summary>
    private ToolStripMenuItem? _openWithItem;

    /// <summary>What the renderer made of the file shown (AP-7); null for Untitled, or in an editor.</summary>
    private ViewerPage? _page;

    /// <summary>The Details list under the text (AP-6); null in an editor, and in a viewer without <see cref="EditorApp.Details"/>.</summary>
    private AxitList? _details;

    /// <summary>The details read last started, so that one coming back after another file was opened is dropped (AP-6.4).</summary>
    private int _detailsRead;

    /// <summary>The edit control as the viewer's text (AP-2.1): read-only, in the window's colours, Tab moving on to the Go to line field and the Details list.</summary>
    private void SetUpViewer()
    {
        // A read-only edit control paints itself in the control colour unless told otherwise.
        _editor.ReadOnly = true;
        _editor.BackColor = SystemColors.Window;
        _editor.ForeColor = SystemColors.WindowText;
        // Tab moves to the Go to line field while it shows, then the Details list; a read-only text has no tab character to type.
        _editor.AcceptsTab = false;
        if (_app.Details is null)
        {
            return;
        }

        // AP-6.1: the bundle's list under the text, a third of the window, always there.
        _details = new AxitList { AccessibleName = "Details", Dock = DockStyle.Bottom, TabIndex = 2 };
        _details.Items.Add("No file");
        _details.SelectedIndex = 0;
        SizeChanged += (_, _) => FitDetails();
    }

    private void FitDetails()
    {
        if (_details is not null)
        {
            _details.Height = Math.Max(ClientSize.Height / 3, _details.ItemHeight * 3);
        }
    }

    /// <summary>The viewer's File menu after Open and the recent files (AP-3.1): nothing is written, so the Save slot holds Reload and the ways on.</summary>
    private void AddViewerFileItems(ToolStripMenuItem file)
    {
        Add(file, new Command("Re&load", EditorKeys.Reload, ReloadCommand));
        file.DropDownItems.Add(new ToolStripSeparator());
        // The item's label follows the file's page (UpdateEditItem); the command's is the key table's, true for any.
        _editItem = Add(file, new Command("&Edit in the editor", EditorKeys.Edit, OpenInEditor) { Mark = CommandMark.Open });
        _openWithItem = new ToolStripMenuItem("Open wit&h");
        CommandPalette.FilledOnOpening(_openWithItem).DropDownOpening += (_, _) => FillOpenWith();
        FillOpenWith();
        file.DropDownItems.Add(_openWithItem);
        UpdateEditItem();
    }

    /// <summary>
    /// The viewer's keys without a menu item: l says the line while the text has the focus (AP-2.3); in the Details
    /// list Ctrl+C copies the line's value and Enter on the Folder line opens it in axFile (AP-6.3). The viewer's other
    /// keys are its File menu's commands (Ctrl+N a new window, F5, Ctrl+E); the shell's keys that write (Ctrl+Shift+S,
    /// Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z) are not made in the viewer, so they do nothing there (AP-3.2).
    /// </summary>
    private void AddViewerKeys()
    {
        // A plain letter: taken in the text only, so that the Go to line field still accepts it.
        AddKey(new Command("Say the line", EditorKeys.LineNumber, SayLine) { When = () => _editor.Focused, Mark = CommandMark.Read });
        AddKey(new Command("Copy the detail's value", EditorKeys.CopyDetail, CopyDetail) { When = () => _details?.Focused == true, Mark = CommandMark.Read });
        AddKey(new Command("Open the detail", EditorKeys.OpenDetail, OpenDetail) { When = () => _details?.Focused == true, Mark = CommandMark.Open });
    }

    private void SayLine()
    {
        var (line, _, lines) = Position();
        Announce($"Line {line} of {lines}", true);
    }

    /// <summary>AP-1.2, AP-1.3: a start with a file while this window is open hands it here; the base answers the message at once and the file is loaded after, so that the starting process never waits on a read.</summary>
    protected override Action? TakeHandOver(HandOver message) =>
        _app.ReadOnly && message is { Verb: AppMessage.Open, Paths: [var path, ..] } ? () => ShowHandedOver(path) : null;

    private void ShowHandedOver(string path)
    {
        // A notice left from before (a refused file) would be what NVDA reads on activation; a notice with a progress
        // bar is the update's, which goes on (the file loads behind it; until 2026-10-23 the hand-over closed it).
        if (NoticeOpen && Overlay.Shown is { Progress: false })
        {
            CloseNotice(null);
        }

        if (LoadDocument(path) && !NoticeOpen)
        {
            // AP-6.1: a file handed over is read from its text, wherever the focus was.
            _editor.Select();
        }

        // The starting process let this one come to the front (AppMessage); the user asked to read.
        AxitMenu.Activate(Handle);
    }

    /// <summary>The viewer's File → New window (Ctrl+N): another window of the app, empty, through the bundle's starter (AX-9); <c>--new</c> keeps it from handing over to this one.</summary>
    private void NewWindow() => AxitMenu.StartWith(_app.Verb, ["--new"]);

    /// <summary>The viewer's File → Reload (F5, AP-2.4): the file read again, the caret kept on its line and column, "Reloaded".</summary>
    private void ReloadCommand()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        Reload("Reloaded");
    }

    /// <summary>
    /// The viewer's Ctrl+E (AP-1.4, AP-7.3), by the file's page: a text in its editor by its kind (a new editor
    /// window), a sound or video in axMedia, any other file in the program Windows has for it.
    /// </summary>
    private void OpenInEditor()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        switch (_page?.WayOut ?? ViewerWayOut.Editor)
        {
            case ViewerWayOut.Editor:
                AxitMenu.Start(Dispatch.EditorOf(_path), _path);
                break;
            case ViewerWayOut.Player:
                AxitMenu.Start(Dispatch.AxMedia, _path);
                break;
            default:
                OpenInWindowsProgram(_path);
                break;
        }
    }

    /// <summary>The program Windows has for the file, as axFile's Enter (AF-3.2); Windows' Open with dialog when it has none.</summary>
    private void OpenInWindowsProgram(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1155)
        {
            // ERROR_NO_ASSOCIATION: Windows asks which program, as File Explorer does.
            ChooseProgram(path);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Fail($"{Path.GetFileName(path)} could not be opened: {ex.Message}");
        }
    }

    /// <summary>Windows' own "How do you want to open this file?" dialog, whose Always sets the default (AP-7.3).</summary>
    private void ChooseProgram(string path)
    {
        if (WindowsOpenWith.Show(Handle, path) is { } reason)
        {
            Fail($"Open with failed: {reason}");
        }
    }

    /// <summary>
    /// File → Open with ▸ (AP-7.3): the Axit apps that are programs for the file's type, axDown for any text, then
    /// Windows' program for it and Windows' own dialog, whose Always is where a default is set; a choice opens the
    /// file once. Filled as the menu opens, from the file shown.
    /// </summary>
    private void FillOpenWith()
    {
        if (_openWithItem is null)
        {
            return;
        }

        _openWithItem.DropDownItems.Clear();
        if (_path is not { } path)
        {
            _openWithItem.DropDownItems.Add(new ToolStripMenuItem("&No file yet") { Enabled = true });
            return;
        }

        var extension = Path.GetExtension(path);
        var apps = Dispatch.Apps.Where(app => app.Verb != _app.Verb
            && (app.OpenWith.Any(type => type == "*" || string.Equals(type, extension, StringComparison.OrdinalIgnoreCase))
                || (app.Verb == Dispatch.AxDown && _page is { IsText: true })));
        // Numbered, the number the mnemonic and a full stop the pause, as the Axit menu's lists (AX-9).
        var number = 0;
        foreach (var app in apps)
        {
            _openWithItem.DropDownItems.Add(new ToolStripMenuItem($"&{++number}. {app.Name}", null, (_, _) => AxitMenu.Start(app.Verb, path)));
        }

        if (_openWithItem.DropDownItems.Count > 0)
        {
            _openWithItem.DropDownItems.Add(new ToolStripSeparator());
        }

        _openWithItem.DropDownItems.Add(new ToolStripMenuItem("&Windows' program for it", null, (_, _) => OpenInWindowsProgram(path)));
        _openWithItem.DropDownItems.Add(new ToolStripMenuItem("&Choose another program...", null, (_, _) => ChooseProgram(path)));
    }

    /// <summary>The viewer's way on, named after the file's page (AP-1.4, AP-7.3); nothing in an editor.</summary>
    private void UpdateEditItem()
    {
        if (_editItem is null)
        {
            return;
        }

        _editItem.Text = (_page?.WayOut ?? ViewerWayOut.Editor) switch
        {
            ViewerWayOut.Player => "Play in ax&Media",
            ViewerWayOut.Windows => "Open in &Windows' program",
            _ => _path is { } path && Dispatch.EditorOf(path) == Dispatch.AxJSON ? "&Edit in axJSON" : "&Edit in axDown",
        };
    }

    // ---- the Details list (AP-6) ----

    /// <summary>
    /// The details of the file shown, read off the UI thread (AP-6.4): "No file" for Untitled; "Reading details" while
    /// another file's are read; the same file's lines kept until the new ones come, so that a reload does not move the
    /// user. The line the user is on is found again by its name.
    /// </summary>
    private async void ReadDetails()
    {
        if (_details is null || _app.Details is not { } details)
        {
            return;
        }

        var read = ++_detailsRead;
        if (_path is not { } path)
        {
            FillDetails(["No file"]);
            return;
        }

        var sameFile = _details.Tag is string shown && string.Equals(shown, path, StringComparison.OrdinalIgnoreCase);
        if (!sameFile)
        {
            FillDetails(["Reading details"]);
        }

        var contents = _page?.Contents ?? [];
        IReadOnlyList<string> lines;
        try
        {
            lines = await Task.Run(() => details(path, contents));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            lines = [$"The details could not be read: {ex.Message}"];
        }

        if (read != _detailsRead || IsDisposed)
        {
            return;
        }

        _details.Tag = path;
        FillDetails(lines);
    }

    /// <summary>The list refilled, the selection on the line of the same name as before, or the first line.</summary>
    private void FillDetails(IReadOnlyList<string> lines)
    {
        if (_details is null)
        {
            return;
        }

        var keep = _details.SelectedItem is string line ? DetailName(line) : null;
        _details.BeginUpdate();
        _details.Items.Clear();
        foreach (var item in lines)
        {
            _details.Items.Add(item);
        }

        var index = keep is null ? -1 : lines.ToList().FindIndex(item => DetailName(item) == keep);
        _details.SelectedIndex = lines.Count == 0 ? -1 : Math.Max(index, 0);
        _details.EndUpdate();
        _details.ResetTypeAhead();
    }

    /// <summary>A detail's name, the text before its first ": "; a heading line's whole text.</summary>
    private static string DetailName(string line) => line.IndexOf(": ", StringComparison.Ordinal) is var at and >= 0 ? line[..at] : line;

    /// <summary>A detail's value, the text after its first ": "; a heading line's whole text.</summary>
    private static string DetailValue(string line) => line.IndexOf(": ", StringComparison.Ordinal) is var at and >= 0 ? line[(at + 2)..] : line;

    /// <summary>Ctrl+C in the Details list (AP-6.3): the line's value, without its name.</summary>
    private void CopyDetail()
    {
        if (_details?.SelectedItem is not string line)
        {
            return;
        }

        try
        {
            Clipboard.SetText(DetailValue(line));
            Announce("Copied", true);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            Announce("The clipboard is busy; try again", true);
        }
    }

    /// <summary>Enter in the Details list (AP-6.3): the Folder line opens axFile on the file, selected in its folder; any other line says so.</summary>
    private void OpenDetail()
    {
        if (_details?.SelectedItem is string line && DetailName(line) == "Folder" && _path is { } path)
        {
            AxitMenu.Start(Dispatch.AxFile, path);
            return;
        }

        Announce("Enter opens the Folder line in axFile", true);
    }

    /// <summary>Ctrl+Tab, Ctrl+Shift+Tab and F6 in the viewer with its list: the text (or the Go to line field) and the Details list in turn.</summary>
    private void SwitchPane()
    {
        if (_details is { Visible: true } details && !details.Focused)
        {
            details.Select();
            return;
        }

        (_area.Current ?? _editor).Select();
    }
}
