using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The viewer's own part of the editor shell (docs/axpreview/SPEC.md AP-D4): what the shell does only in its
/// read-only mode, axPreview. The app names here (Edit in axDown, Edit in axJSON, the <c>.json</c> rule) are
/// axPreview's rule of which editor opens a file (AP-1.4), written where the viewer lives rather than in the
/// viewer's data, since the shell makes the menu item. In its own file since 2026-10-23 (review 5.6).
/// </summary>
public sealed partial class TextEditorForm
{
    /// <summary>File → Edit in axDown or axJSON, named after the file's kind; null in an editor.</summary>
    private ToolStripMenuItem? _editItem;

    /// <summary>The edit control as the viewer's text (AP-2.1): read-only, in the window's colours, Tab moving on to the Go to line field.</summary>
    private void SetUpViewer()
    {
        // A read-only edit control paints itself in the control colour unless told otherwise.
        _editor.ReadOnly = true;
        _editor.BackColor = SystemColors.Window;
        _editor.ForeColor = SystemColors.WindowText;
        // Tab moves to the Go to line field while it shows; a read-only text has no tab character to type.
        _editor.AcceptsTab = false;
    }

    /// <summary>The viewer's File menu after Open and the recent files (AP-3.1): nothing is written, so the Save slot holds Reload and the way to the editor.</summary>
    private void AddViewerFileItems(ToolStripMenuItem file)
    {
        file.DropDownItems.Add(Item("Re&load", ReloadCommand, EditorKeys.Reload));
        file.DropDownItems.Add(new ToolStripSeparator());
        _editItem = Item("&Edit in axDown", OpenInEditor, EditorKeys.Edit);
        file.DropDownItems.Add(_editItem);
    }

    /// <summary>
    /// The viewer's keys (AP-3.2), asked before the shell's: Ctrl+N is a new window, F5 reloads, Ctrl+E opens the
    /// editor, l says the line while the text has the focus; the shell's keys that write do nothing. Null for a key
    /// that is not the viewer's, true when handled, false for one the viewer turns off.
    /// </summary>
    private bool? ViewerKey(Keys keyData)
    {
        switch (keyData)
        {
            case EditorKeys.NewDocument:
                NewWindow();
                return true;
            case EditorKeys.Reload:
                ReloadCommand();
                return true;
            case EditorKeys.Edit:
                OpenInEditor();
                return true;
            case EditorKeys.LineNumber when _editor.Focused:
                // A plain letter: taken in the text only, so that the Go to line field still accepts it.
                var (line, _, lines) = Position();
                Announce($"Line {line} of {lines}", true);
                return true;
            case EditorKeys.SaveAs:
            case EditorKeys.Undo:
            case EditorKeys.Redo:
            case EditorKeys.RedoAlso:
                return false;
        }

        return null;
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

        LoadDocument(path);
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

    /// <summary>The viewer's File → Edit in axDown or axJSON (Ctrl+E, AP-1.4): the file in its editor by its kind, a new editor window.</summary>
    private void OpenInEditor()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(Dispatch.EditorOf(_path), _path);
    }

    /// <summary>The viewer's way to the editor, named after the file's kind (AP-1.4); nothing in an editor.</summary>
    private void UpdateEditItem()
    {
        if (_editItem is not null)
        {
            _editItem.Text = _path is { } path && Dispatch.EditorOf(path) == Dispatch.AxJSON ? "&Edit in axJSON" : "&Edit in axDown";
        }
    }
}
