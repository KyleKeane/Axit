using Axit.Core;
using Axit.Forms;
using AxJSON.Core;

namespace AxJSON;

/// <summary>
/// axJSON's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AJ-1): <c>Axit.exe json [&lt;file&gt;]</c>
/// calls <see cref="Run"/>, which hands the editor shell (<c>Axit.Forms.EditorHost</c>, <c>TextEditorForm</c>) what
/// axJSON is: its name and verb, its settings file, its file types, its texts, its JSON keys, its resources and its
/// Format command (AJ-D4).
/// </summary>
public static class AxJSONApp
{
    public const string Usage = """
        Usage: Axit json [<file>]
               axjson [<file>]  the same, as the installed console command

          <file>            the file to open; a path that does not exist yet is a new document with that name
          --help, --version this text, or the version
        """;

    /// <summary>axJSON as the editor shell sees it (bundle AX-6.2, AJ-D4).</summary>
    public static readonly EditorApp App = new(
        Name: "axJSON",
        Verb: Dispatch.AxJSON,
        SettingsName: "axjson",
        FileFilter: "JSON files (*.json;*.jsonc)|*.json;*.jsonc|All files (*.*)|*.*",
        DefaultExtension: "json",
        Shortcuts: HelpText.Shortcuts,
        UserGuide: HelpText.UserGuide,
        Jumps: AxJSONKeys.Jumps,
        ResourcesLabel: "JSON &resources",
        Resources:
        [
            ("&JSON syntax", "https://www.json.org/json-en.html"),
            ("Claude Code &settings", "https://code.claude.com/docs/en/settings"),
        ],
        Commands:
        [
            new EditorCommand("&Format", AxJSONKeys.Format, text =>
            {
                var (formatted, message) = JsonFormat.Apply(text);
                return new Rewrite(formatted, message);
            }),
        ]);

    public static void Run(string[] args) => EditorHost.Run(App, Usage, args);
}
