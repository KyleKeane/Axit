using Axit.Core;
using Axit.Forms;

namespace AxDown;

/// <summary>
/// axDown's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AD-1.4): <c>Axit.exe down [&lt;file&gt;]</c>
/// calls <see cref="Run"/>, which hands the editor shell (<c>Axit.Forms.EditorHost</c>, <c>TextEditorForm</c>) what
/// axDown is: its name and verb, its settings file, its file types, its texts, its Markdown keys and its resources.
/// </summary>
public static class AxDownApp
{
    public const string Usage = """
        Usage: Axit down [<file>]
               axdown [<file>]  the same, as the installed console command

          <file>            the file to open; a path that does not exist yet is a new document with that name
          --help, --version this text, or the version
        """;

    /// <summary>axDown as the editor shell sees it (bundle AX-6.2, AJ-D4).</summary>
    public static readonly EditorApp App = new(
        Name: "axDown",
        Verb: Dispatch.AxDown,
        SettingsName: "axdown",
        FileFilter: "Markdown and text files (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|All files (*.*)|*.*",
        DefaultExtension: "md",
        Shortcuts: HelpText.Shortcuts,
        UserGuide: HelpText.UserGuide,
        Jumps: AxDownKeys.Jumps,
        ResourcesLabel: "Markdown &resources",
        Resources:
        [
            ("Markdown &basics", "https://commonmark.org/help/"),
            ("&GitHub Flavored Markdown", "https://github.github.com/gfm/"),
        ],
        Commands: []);

    public static void Run(string[] args) => EditorHost.Run(App, Usage, args);
}
