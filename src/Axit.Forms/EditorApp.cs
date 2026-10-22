namespace Axit.Forms;

/// <summary>
/// A text editor app of the bundle as the editor shell sees it (docs/axit/SPEC.md AX-6.2; docs/axjson/SPEC.md AJ-D4):
/// everything that differs between AxDown and AxJSON, as data. <see cref="TextEditorForm"/> is the window of both;
/// <see cref="EditorHost.Run"/> starts one on this record.
/// </summary>
/// <param name="Name">The app's name, the end of its window titles ("notes.md - AxDown").</param>
/// <param name="Verb">The verb <c>Axit.exe</c> takes for it (<c>Axit.Core.Dispatch</c>).</param>
/// <param name="SettingsName">The settings file's and the log's name, "axdown" for <c>axdown.json</c> and <c>axdown.log</c>.</param>
/// <param name="FileFilter">The Open and Save dialogs' filter.</param>
/// <param name="DefaultExtension">The extension Save as adds to a new document's name, "md".</param>
/// <param name="Shortcuts">The F1 text.</param>
/// <param name="UserGuide">The embedded user guide, read when Help → User guide is chosen.</param>
/// <param name="Jumps">The key pairs that move through the text by its structure, for the editor and every notice text (AX-7.2).</param>
/// <param name="ResourcesLabel">The Help submenu of pages about the file format, "Markdown &amp;resources".</param>
/// <param name="Resources">Its items: a label with a mnemonic and the page it opens in the browser.</param>
/// <param name="Commands">The app's own commands on the whole text, at the end of the Edit menu: AxJSON's Format.</param>
/// <param name="ReadOnly">A viewer (AxPreview, docs/axpreview/SPEC.md AP-D4): the text is read-only, there is no Edit menu, no Save and no history; the File menu has Reload and Edit in the editor instead; the window keeps one instance and takes a file handed over by a start (<see cref="AppMessage"/>).</param>
/// <param name="Refuse">For a viewer: the notice for a file that is not opened (not text, too large), or null when it opens; the window then keeps what it had (AP-2.2).</param>
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
    Func<string, Notice?>? Refuse = null);

/// <summary>A command of an editor app on the whole text (AJ-2.3): its menu label with a mnemonic, its key from the app's table, and the function that rewrites the text.</summary>
public sealed record EditorCommand(string Label, Keys Key, Func<string, Rewrite> Run);

/// <summary>What a command made of the text, and the one announcement for it ("Formatted", "Not valid JSON: line 12, expected a comma"); the text unchanged when nothing could be done.</summary>
public sealed record Rewrite(string Text, string Message);
