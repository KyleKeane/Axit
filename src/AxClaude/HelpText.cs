using System.Reflection;
using AxClaude.Core.Pty;

namespace AxClaude;

/// <summary>The texts the window shows as notices: the keyboard shortcuts (F1), the user guide, and the help when Claude Code is missing.</summary>
internal static class HelpText
{
    public const string Shortcuts = """
        Anywhere
          Ctrl+1: go to the message field. Ctrl+2: go to the conversation. Both say where you are, also when you are there already
          Ctrl+3: go to the latest reply, in the conversation
          Tab, Shift+Tab, Ctrl+Tab or F6: switch between the message field (or Claude's question in its place) and the conversation
          Shift+Escape: interrupt Claude, or close one of Claude's dialogs
          Ctrl+Shift+S: make Claude take up the waiting message now instead of after its current step
          Ctrl+F: find text in the conversation. F3 and Shift+F3: next or previous match
          Ctrl+S: save the conversation as a text file
          Ctrl+W: show the current folder in File Explorer
          Ctrl+N: new session: choose the folder and how Claude starts (continue, resume, plan mode, custom arguments)
          Ctrl+Shift+R: force Claude to restart in the same folder with the same arguments, after a confirmation
          Ctrl+O: switched off, a reminder says why (Claude's detailed view redraws the conversation in screen reader mode)
          Ctrl+Shift+C: send Ctrl+C to Claude
          Ctrl+Shift+M: next permission mode
          Ctrl+Alt and a letter from the Conversation list below: read every line of that kind from the caret to the end of the conversation in one go, without moving; Ctrl+Alt+P reads the paragraphs whole. Control stops NVDA
          Ctrl+Shift+K: bookmark the conversation line the caret is on, or take the bookmark away
          Ctrl+Space: background music on or off, and says which
          Ctrl+Q: read Claude's question or screen again while it waits at the bottom, all its lines and the answer the focus is on, without moving
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          F1: this list
          Alt+A: the Axit menu: a new AxClaude or AxDown window, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About
          Alt+F4: close AxClaude (asks first while Claude is working)

        Message field
          Enter: send (an empty field sends a plain Enter to Claude)
          Shift+Enter: new line
          Page Up, Page Down: one screen of the field up or down, or the first or last line of the message
          Escape: does nothing; a reminder says that Shift+Escape interrupts Claude
          Ctrl+Up, Ctrl+Down: the previous or next paragraph of the message; with Shift, select to there

        Conversation
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End: move as in any text
          Page Up, Page Down: one screen up or down
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line); with Shift, select to there
          l: say the line number, like "Line 12 of 340"
          i and Shift+I: next and previous "# Input" line (a message you sent)
          o and Shift+O, or r and Shift+R: next and previous "# Output" line (a reply from Claude)
          h and Shift+H: next and previous heading. 1 to 6: heading of that level
            (Input and Output lines are level 1, Claude's headings level 2 and deeper)
            Both work in the user guide and the other help texts too, by their Markdown headings
          c and Shift+C: next and previous "claude:" line
          t and Shift+T: next and previous tool line
          p and Shift+P: next and previous paragraph (the first line after a blank line, or a "claude:" line; never a tool or thinking line)
          e and Shift+E: next and previous error or warning
          d and Shift+D: next and previous "done" line at the end of a reply
          s and Shift+S: next and previous system line
          q and Shift+Q: next and previous question Claude asked (its "Question:" line)
          m: bookmark this line (a "Bookmark 1" line appears above it), or take the bookmark away again
          k and Shift+K: next and previous bookmark, read together with the line it marks
          Enter: copy the line into the message field under a marker line and go there, to comment on it
          Backspace: back to the line you were on before the last jump; again for the jump before that
          Ctrl+End, then Shift and a letter: the last line of that kind
          Shift with the arrow keys, or Ctrl+A: select text. Ctrl+C: copy the selection. New output waits while text is selected
          Escape: clear the selection, or go to the message field

        When Claude asks something
          A list of the answers takes the message field's place; the question is read each time you arrive there
          Tab, Shift+Tab, Ctrl+Tab or F6: between the conversation and the answers
          Up, Down, or an answer's number: choose (the number again: the next answer starting with it; Shift and the number: back; 0: answers 1 to 9)
          Page Up, Page Down: ten answers. Home, End: the first or last. Space: tick, when several answers are allowed
          Enter: answer. Escape: cancel the question, or go back a step
          The message field comes back when Claude has your answer

        Notices
          AxClaude's questions, errors and help texts appear inside the window, in place of the conversation.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice.

        Claude Code itself
          Type /help in the message field and press Enter for Claude Code's own commands.
          The Help menu opens the Claude Code documentation in your browser.
        """;

    /// <summary>docs/axclaude/user-guide.md, embedded in the executable at build time.</summary>
    public static string UserGuide()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UserGuide.md");
        if (stream is null)
        {
            return "The user guide is not included in this build. It is docs/axclaude/user-guide.md in the source repository.";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>FR-1.9: where the app looked, the install command, and the ways back.</summary>
    public static string ClaudeNotFound(IReadOnlyList<string> candidates)
    {
        var lines = new List<string>
        {
            "Claude Code was not found, so AxClaude cannot start it.",
            string.Empty,
            "AxClaude looked here:",
        };
        lines.AddRange(candidates.Select(c => "  " + c));
        lines.AddRange(
        [
            string.Empty,
            "To install Claude Code, open Windows PowerShell and run this command (the Copy install command button copies it):",
            "  " + ClaudeLauncher.InstallCommand,
            string.Empty,
            "Then run claude once in PowerShell to log in. Come back here, close this notice and press Ctrl+Shift+R.",
            string.Empty,
            "If Claude Code is installed somewhere else, use Locate claude.exe. AxClaude remembers the path.",
            string.Empty,
            "Install page: " + ClaudeLauncher.InstallUrl,
        ]);
        return string.Join("\n", lines);
    }
}
