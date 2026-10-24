using System.Reflection;
using Axit.Core;

namespace AxCalc;

/// <summary>The texts axCalc shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AC-7.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axCalc's own, what an expression takes. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axCalc",
        [
            "Tab, Shift+Tab, Ctrl+Tab or F6: switch between the history and the expression field",
            "Ctrl+F: find an entry in the history. F3 and Shift+F3: next or previous match",
            "Ctrl+S: save the history as a text file",
        ]) + """
        ## axCalc
          In the expression field
            Enter: the answer, spoken; the line joins the history and the field empties
            Escape: clear the field
          In the history
            Enter: the entry's expression back in the field, to change it
            Delete: remove the entry
            Escape: back to the field
          Ctrl+Shift+C: copy the answer (the last one, or the entry's in the history)
          Ctrl+I: the answer in hexadecimal, binary and octal
          Ctrl+Q: say the last answer again
          Ctrl+Shift+Delete: clear the history, after a question

        ## Expressions
          + - * / as usual, % or mod for the remainder, ^ or ** for a power, ! for a factorial, parentheses to group
          0xFF, 0b1010 and 0o17 for hexadecimal, binary and octal; 1_000_000 with underscores; 6.02e23
          Functions: sqrt, cbrt, abs, exp, ln, log, log2, sin, cos, tan, asin, acos, atan, floor, ceil, trunc, round, min, max, hypot
          Names: pi, e, ans (the last answer); name = 5 sets a variable
          Starting with an operator continues from the last answer: *2 doubles it. A leading minus is a negative number
        """;

    /// <summary>docs/axcalc/user-guide.md, embedded by AxCalc.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axcalc/user-guide.md");
}
