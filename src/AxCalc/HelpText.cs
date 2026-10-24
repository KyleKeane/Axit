using System.Reflection;
using Axit.Core;

namespace AxCalc;

/// <summary>The texts axCalc shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AC-7.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axCalc's own, what an expression takes.</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Tab, Shift+Tab, Ctrl+Tab or F6: switch between the history and the expression field
          Ctrl+F: find an entry in the history. F3 and Shift+F3: next or previous match
          Ctrl+S: save the history as a text file
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close axCalc
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, the AI tools, Check for updates, and About

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
