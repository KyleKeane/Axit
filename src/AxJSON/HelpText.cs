using Axit.Core;
using System.Reflection;

namespace AxJSON;

/// <summary>The texts AxJSON shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
internal static class HelpText
{
    /// <summary>The F1 text (SPEC.md AJ-2.2, AJ-2.3). Bundle-wide keys first, then the editor shell's and AxJSON's own, then what the edit control does itself.</summary>
    public const string Shortcuts = """
        In every window of Axit
          F1: this list
          Ctrl+F: find text. F3 and Shift+F3: next or previous match
          Ctrl+S: save the file
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxJSON (asks first when there are unsaved changes)
          Alt+A: the Axit menu: a new AxJSON, AxDown or AxClaude window, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        AxJSON
          Ctrl+N: new, empty document
          Ctrl+O: open a file
          Ctrl+Shift+S: save under a new name
          Ctrl+W: show the current file in File Explorer
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          Ctrl+K and Ctrl+Shift+K: next and previous key, spoken with its value ("model: opus") or its size ("permissions, object with 2 keys")
          Ctrl+J and Ctrl+Shift+J: next and previous object, spoken with its key and its number of keys
          Ctrl+L and Ctrl+Shift+L: next and previous array, spoken with its key and its number of items
          Ctrl+I and Ctrl+Shift+I: next and previous item of an array, "Item 2 of 5, Read"
          Ctrl+E and Ctrl+Shift+E: the place where the file stops being JSON, with the line and what was expected there; "No next error" means the file is valid
            All of them work in the user guide and the other notices too
          Ctrl+Shift+F: format: two spaces per level, one key per line; a trailing comma, a missing comma, single quotes, an unquoted key and a comment are repaired on the way and named
          Ctrl+Shift+W: word wrap on or off

        The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C, Ctrl+X, Ctrl+V: copy, cut, paste. Ctrl+Z: undo
          Enter: a new line. Tab: a tab character

        Notices
          AxJSON's questions, errors and help texts appear inside the window, in place of the text.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice.
        """;

    /// <summary>docs/axjson/user-guide.md, embedded by AxJSON.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axjson/user-guide.md");
}
