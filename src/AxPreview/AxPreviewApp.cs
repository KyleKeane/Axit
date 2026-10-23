using Axit.Core;
using Axit.Forms;
using AxPreview.Core;

namespace AxPreview;

/// <summary>
/// axPreview's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AP-1.1, AP-1.2): <c>Axit.exe preview
/// [&lt;file&gt;] [--new]</c> calls <see cref="Run"/>, which hands the text editor shell (<c>Axit.Forms.TextEditorForm</c>)
/// what axPreview is: the editor shell read-only (AP-D4), with axDown's navigation keys, the viewer's own keys and
/// texts, and the rule that refuses a file that is not text. With a file and an open axPreview window, the file is
/// handed to that window (<see cref="AppMessage"/>) and no window of this process opens.
/// </summary>
public static class AxPreviewApp
{
    public const string Name = "axPreview";

    public const string Usage = """
        Usage: Axit preview [<file>] [--new]
               axpreview [<file>]  the same, as the installed console command

          <file>            the file to read; without one, an empty window
          --new             a window of its own, even while an axPreview window is open (the open window takes the file otherwise)
          --help, --version this text, or the version
        """;

    /// <summary>axPreview as the editor shell sees it (bundle AX-6.2): the shell read-only, axDown's keys and resources.</summary>
    public static readonly EditorApp App = new(
        Name: Name,
        Verb: Dispatch.AxPreview,
        SettingsName: "axpreview",
        FileFilter: "Text files (*.txt;*.md;*.markdown;*.log;*.json)|*.txt;*.md;*.markdown;*.log;*.json|All files (*.*)|*.*",
        DefaultExtension: "txt",
        Shortcuts: HelpText.Shortcuts,
        UserGuide: HelpText.UserGuide,
        Jumps: AxPreviewKeys.Jumps,
        ResourcesLabel: "Markdown &resources",
        Resources:
        [
            ("Markdown &basics", "https://commonmark.org/help/"),
            ("&GitHub Flavored Markdown", "https://github.github.com/gfm/"),
        ],
        Commands: [],
        ReadOnly: true,
        Refuse: path => TextSniff.Refuse(path) is { } refusal ? Notice.Plain(refusal.Title, refusal.Text) : null);

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axpreview", Usage, args, arguments =>
        {
            var fresh = arguments.Contains("--new", StringComparer.Ordinal);
            if (!AppHost.OnePath(Name, "file", Usage, arguments.Where(argument => argument != "--new").ToArray(), out var path))
            {
                return null;
            }

            if (path is not null && !fresh && AppMessage.Send(Name, AppMessage.Open, [path]))
            {
                // AP-1.2: one viewer; the open window has the file and this process ends.
                return null;
            }

            var settings = EditorSettings.Load(AppPaths.SettingsFile(App.SettingsName), out var settingsError);
            return new TextEditorForm(App, path, settings, settingsError);
        });
}
