using System.Reflection;
using Axit.Core;

namespace AxClip;

/// <summary>The texts axClip shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md CP-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axClip",
        [
            "Ctrl+F, F3 and Shift+F3: find in a notice's text",
        ],
        altF4: "hide axClip (Quit axClip, in its first menu, ends it)") + """
        ## axClip
          Windows+Alt+V, or the hotkey you chose: show axClip from anywhere; again: hide it
          Type: only the items that hold every word typed
          Down, Up: the next or the previous item. Ctrl+Home, Ctrl+End: the first or the last. Page Down, Page Up: a screen
          Enter: paste the item into the program you were in
          Shift+Enter: put the item on the clipboard without pasting
          Ctrl+Q: read the item in full
          Delete: remove the item. Ctrl+Shift+Delete: clear the history, after a question
          Ctrl+P: pause recording, or record again
          Escape: hide axClip
        """;

    /// <summary>docs/axclip/user-guide.md, embedded by AxClip.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axclip/user-guide.md");
}
