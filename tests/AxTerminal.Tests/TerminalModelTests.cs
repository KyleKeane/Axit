using AxTerminal.Core;
using Axit.Core.Transcript;
using static AxTerminal.Tests.TestHelpers;

namespace AxTerminal.Tests;

/// <summary>The model on hand-written streams with the marks a shell's prompt emits (SPEC.md AT-1.4, AT-3.2, AT-4.2, AT-5.1).</summary>
public sealed class TerminalModelTests
{
    /// <summary>A prompt as the PowerShell snippet prints it: D (with a code after a command), A, the directory, the text, B.</summary>
    private static string Prompt(string? code, string directory = "C:\\src") =>
        $"{Esc}]133;D{(code is null ? string.Empty : ";" + code)}{Bel}{Esc}]133;A{Bel}{Esc}]9;9;\"{directory}\"{Bel}PS {directory}> {Esc}]133;B{Bel}";

    [Fact]
    public void Prompt_rows_are_hidden_and_the_block_goes_above_them()
    {
        var model = new TerminalModel(80, 10);
        var ready = 0;
        var finished = new List<int?>();
        model.Ready += () => ready++;
        model.CommandFinished += finished.Add;

        Feed(model, Prompt(null));
        model.EndFrame();
        Assert.Equal(1, ready);
        Assert.True(model.HasMarks);
        Assert.False(model.Running);
        Assert.Equal("PS C:\\src>", model.Prompt);
        Assert.Equal("C:\\src", model.Directory);
        Assert.Equal(string.Empty, Render(model));

        Assert.True(model.Send("echo hello"));
        Assert.True(model.Running);
        Assert.Equal("echo hello", model.RunningCommand);
        // The shell echoes the command on the prompt row, prints the output and a new prompt.
        Feed(model, $"echo hello\r\nhello\r\n{Prompt("0")}");
        model.EndFrame();

        Assert.Equal([0], finished);
        Assert.False(model.Running);
        Assert.Equal(1, ready);
        Assert.Equal("# Input 1\r\n## Folder\r\nC:\\src\r\n## Command\r\necho hello\r\n\r\n# Output 1\r\n\r\nhello", Render(model));
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "echo hello", "", "# Output 1", "", "hello"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
        Assert.Equal(LineKind.InputMarker, model.Lines[0].Kind);
        Assert.Equal(1, model.Lines[0].HeadingLevel);
    }

    [Fact]
    public void A_failing_command_reports_its_code_and_a_bare_enter_nothing()
    {
        var model = new TerminalModel(80, 10);
        var finished = new List<int?>();
        model.CommandFinished += finished.Add;
        Feed(model, Prompt(null));
        model.EndFrame();

        Assert.False(model.Send("  \n"));
        Feed(model, $"\r\n{Prompt(null)}");
        model.EndFrame();
        Assert.Empty(finished);
        Assert.Equal(string.Empty, Render(model));

        model.Send("cmd /c exit 3");
        Feed(model, $"cmd /c exit 3\r\n{Prompt("3")}");
        model.EndFrame();
        Assert.Equal([3], finished);

        // Command Prompt gives a D without a code.
        model.Send("echo x");
        Feed(model, $"echo x\r\nx\r\n{Prompt(null)}");
        model.EndFrame();
        Assert.Equal([3, null], finished);
    }

    [Fact]
    public void An_answer_to_a_running_program_goes_after_its_output()
    {
        var model = new TerminalModel(80, 10);
        Feed(model, Prompt(null));
        model.EndFrame();
        model.Send("ask");
        Feed(model, "ask\r\nContinue? (y/n): ");
        model.EndFrame();
        Assert.True(model.Running);
        Assert.Equal("Continue? (y/n):", model.CursorRowText);

        model.Send("y");
        Assert.Equal("ask", model.RunningCommand);
        Feed(model, $"y\r\ndone\r\n{Prompt("0")}");
        model.EndFrame();
        Assert.Equal(
            ["# Input 1", "## Folder", "C:\\src", "## Command", "ask", "", "# Output 1", "", "Continue? (y/n): y", "# Input 2", "## Folder", "C:\\src", "## Command", "y", "", "# Output 2", "", "done"],
            model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
        Assert.Null(model.CursorRowText);
    }

    [Fact]
    public void Without_marks_nothing_is_hidden_and_there_is_no_state()
    {
        var model = new TerminalModel(80, 10);
        Feed(model, "C:\\>");
        model.EndFrame();
        Assert.False(model.HasMarks);
        Assert.Equal("C:\\>", Render(model));
        model.Send("dir");
        Assert.False(model.Running);
        Feed(model, "dir\r\na.txt\r\nC:\\>");
        model.EndFrame();
        // The echo lands on the prompt row, which nothing hides here, and the block was placed after it.
        Assert.Equal(["C:\\>dir", "# Input 1", "## Command", "dir", "", "# Output 1", "", "a.txt", "C:\\>"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
    }

    [Fact]
    public void Clear_empties_the_window_and_the_prompt_row_is_hidden_again()
    {
        var model = new TerminalModel(80, 10);
        Feed(model, Prompt(null));
        model.EndFrame();
        model.Send("echo one");
        Feed(model, $"echo one\r\none\r\n{Prompt("0")}");
        model.EndFrame();
        model.AddSystemLine("note");
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "echo one", "", "# Output 1", "", "one", "System: note"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));

        model.Clear();
        Assert.DoesNotContain(model.Lines, l => !l.Hidden);
        Assert.Equal(0, model.ExchangeCount);

        model.Send("echo two");
        Feed(model, $"echo two\r\ntwo\r\n{Prompt("0")}");
        model.EndFrame();
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "echo two", "", "# Output 1", "", "two"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
    }

    [Fact]
    public void Output_written_over_an_old_prompt_row_shows()
    {
        var model = new TerminalModel(80, 6);
        Feed(model, Prompt(null));
        model.EndFrame();
        model.Send("cls");
        // cls: the screen is erased and the prompt is printed on the top row; the old prompt row (row 0 too here) is
        // rewritten by the prompt, then output fills the rows below.
        Feed(model, $"cls\r\n{Esc}[2J{Esc}[H{Prompt("0")}");
        model.EndFrame();
        model.Send("echo a");
        Feed(model, $"echo a\r\na\r\nb\r\nc\r\nd\r\n{Prompt("0")}");
        model.EndFrame();
        var visible = model.Lines.Where(l => !l.Hidden).Select(l => l.Text).ToList();
        Assert.Contains("a", visible);
        Assert.Contains("d", visible);
        Assert.DoesNotContain(visible, t => t.StartsWith("PS C:", StringComparison.Ordinal));
    }

    [Fact]
    public void Continuation_rows_of_a_multi_line_command_are_hidden()
    {
        var model = new TerminalModel(80, 10);
        Feed(model, Prompt(null));
        model.EndFrame();
        model.Send("if ($true) {\n\"yes\"\n}");
        Feed(model, $"if ($true) {{\r\n>> \"yes\"\r\n>> }}\r\n>>\r\nyes\r\n{Prompt("0")}");
        model.EndFrame();
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "if ($true) {", "\"yes\"", "}", "", "# Output 1", "", "yes"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
    }

    [Fact]
    public void Bookmarks_and_reset()
    {
        var model = new TerminalModel(80, 10);
        Feed(model, Prompt(null));
        model.EndFrame();
        model.Send("echo one");
        Feed(model, $"echo one\r\none\r\n{Prompt("0")}");
        model.EndFrame();
        var one = model.Lines.First(l => l.Text == "one");
        var added = model.ToggleBookmark(one);
        Assert.True(added?.Added);
        Assert.Equal("Bookmark 1", added?.Bookmark.Text);
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "echo one", "", "# Output 1", "", "Bookmark 1", "one"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
        Assert.False(model.ToggleBookmark(one)?.Added);

        var ready = 0;
        model.Ready += () => ready++;
        model.ResetScreen();
        Assert.False(model.HasMarks);
        Assert.Null(model.Prompt);
        Feed(model, Prompt(null, "D:\\other"));
        model.EndFrame();
        Assert.Equal(1, ready);
        Assert.Equal("PS D:\\other>", model.Prompt);
        Assert.Equal(["# Input 1", "## Folder", "C:\\src", "## Command", "echo one", "", "# Output 1", "", "one"], model.Lines.Where(l => !l.Hidden).Select(l => l.Text));
    }

    [Fact]
    public void Arrivals_tick_once_and_speak_final_lines_once()
    {
        var model = new TerminalModel(80, 10);
        var arrivals = new TerminalArrivals();
        Feed(model, Prompt(null));
        model.EndFrame();
        Assert.Equal(new TerminalArrival(false, null), arrivals.Update(model.Lines, model.Running, speak: true));

        model.Send("echo hello");
        Feed(model, "echo hello\r\nhello\r\nworld");
        model.EndFrame();
        var first = arrivals.Update(model.Lines, model.Running, speak: true);
        Assert.True(first.NewLine);
        // "world" sits on the cursor row with nothing after it and the command running: not final yet.
        Assert.Equal("hello", first.Speech);

        Feed(model, $"\r\n{Prompt("0")}");
        model.EndFrame();
        var second = arrivals.Update(model.Lines, model.Running, speak: true);
        Assert.False(second.NewLine);
        Assert.Equal("world", second.Speech);
        Assert.Equal(new TerminalArrival(false, null), arrivals.Update(model.Lines, model.Running, speak: true));
    }
}
