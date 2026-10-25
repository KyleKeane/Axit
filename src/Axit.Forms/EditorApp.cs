namespace Axit.Forms;

/// <summary>
/// A text editor app of the bundle as the editor shell sees it (docs/axit/SPEC.md AX-6.2; docs/axjson/SPEC.md AJ-D4):
/// everything that differs between axDown and axJSON, as data. <see cref="TextEditorForm"/> is the window of both;
/// <see cref="EditorHost.Run"/> starts one on this record.
/// </summary>
/// <param name="Name">The app's name, the end of its window titles ("notes.md - axDown").</param>
/// <param name="Verb">The verb <c>Axit.exe</c> takes for it (<c>Axit.Core.Dispatch</c>).</param>
/// <param name="SettingsName">The settings file's and the log's name, "axdown" for <c>axdown.json</c> and <c>axdown.log</c>.</param>
/// <param name="FileFilter">The Open and Save dialogs' filter.</param>
/// <param name="DefaultExtension">The extension Save as adds to a new document's name, "md".</param>
/// <param name="Shortcuts">The F1 text.</param>
/// <param name="UserGuide">The embedded user guide, read when Help → User guide is chosen.</param>
/// <param name="Jumps">The key pairs that move through the text by its structure, for the editor and every notice text (AX-7.2).</param>
/// <param name="ResourcesLabel">The Help submenu of pages about the file format, "Markdown &amp;resources".</param>
/// <param name="Resources">Its items: a label with a mnemonic and the page it opens in the browser.</param>
/// <param name="Commands">The app's own commands on the whole text, at the end of the Edit menu: axJSON's Format.</param>
/// <param name="ReadOnly">A viewer (axPreview, docs/axpreview/SPEC.md AP-D4): the text is read-only, there is no Edit menu, no Save and no history; the File menu has Reload and Edit in the editor instead; the window keeps one instance and takes a file handed over by a start (<see cref="AppMessage"/>).</param>
/// <param name="Refuse">For a viewer: the notice for a file that is not opened (a text too large), or null when it opens; the window then keeps what it had (AP-2.2).</param>
/// <param name="Render">For a viewer: what a file shows, from the renderer that takes it (AP-7); null reads every file as text.</param>
/// <param name="Details">For a viewer: the Details list's lines of a file, given its page's Contents lines (AP-6), run off the UI thread; null for a window without the list.</param>
public sealed record EditorApp(
    string Name,
    string Verb,
    string SettingsName,
    string FileFilter,
    string DefaultExtension,
    string Shortcuts,
    Func<string> UserGuide,
    IReadOnlyList<TextJump> Jumps,
    string ResourcesLabel,
    IReadOnlyList<(string Label, string Url)> Resources,
    IReadOnlyList<EditorCommand> Commands,
    bool ReadOnly = false,
    Func<string, Notice?>? Refuse = null,
    Func<string, ViewerPage>? Render = null,
    Func<string, IReadOnlyList<string>, IReadOnlyList<string>>? Details = null);

/// <summary>Where the viewer's Ctrl+E goes (docs/axpreview/SPEC.md AP-7.3): the file's editor, axMedia, or the program Windows has for it.</summary>
public enum ViewerWayOut
{
    Editor,
    Player,
    Windows,
}

/// <summary>What the viewer shows of a file (AP-7.1): the text as a document, the renderer's name for the status bar, the lines of the details' Contents group, and the way out; a text file's way out is its editor.</summary>
public sealed record ViewerPage(Axit.Core.TextDocument Document, string Kind, IReadOnlyList<string> Contents, ViewerWayOut WayOut)
{
    /// <summary>A text file shown as itself, whose status bar names its encoding and line ending.</summary>
    public bool IsText => WayOut == ViewerWayOut.Editor;
}

/// <summary>A command of an editor app on the whole text (AJ-2.3): its menu label with a mnemonic, its key from the app's table, and the function that rewrites the text.</summary>
public sealed record EditorCommand(string Label, Keys Key, Func<string, Rewrite> Run);

/// <summary>What a command made of the text, and the one announcement for it ("Formatted", "Not valid JSON: line 12, expected a comma"); the text unchanged when nothing could be done.</summary>
public sealed record Rewrite(string Text, string Message);
