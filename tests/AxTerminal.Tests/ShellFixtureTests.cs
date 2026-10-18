using AxTerminal.Core;
using static AxTerminal.Tests.TestHelpers;

namespace AxTerminal.Tests;

/// <summary>
/// The recorded shell sessions (tests/fixtures/shell-*.vt, made with PtyCapture --shell and the scripts next to them)
/// through the model: the transcript as the reader sees it against its expected file, the marks' state, and the
/// same transcript whatever the chunk boundaries. Set AXTERMINAL_UPDATE_EXPECTED=1 to regenerate the expected files
/// after a deliberate change, and review the diff.
/// </summary>
public sealed class ShellFixtureTests
{
    public const string PowerShell = "shell-powershell5.1-240x50.vt";
    public const string Cmd = "shell-cmd-240x50.vt";

    /// <summary>The prompt's B mark with its terminator, so that the mark is complete before the command goes: PowerShell's snippet ends it with BEL, cmd's PROMPT with ESC backslash.</summary>
    private const string PowerShellPrompt = "\\e]133;B\\a";
    private const string CmdPrompt = "\\e]133;B\\e\\";

    /// <summary>The script's sends, each after what the script waited for.</summary>
    private static readonly (string Trigger, string Command)[] PowerShellSends =
    [
        (PowerShellPrompt, "Write-Output hello"),
        (PowerShellPrompt, "cmd /c exit 3"),
        (PowerShellPrompt, "Get-ChildItem -Name"),
        (PowerShellPrompt, "Read-Host \"Continue? (y/n)\""),
        // The question row, not the command's echo, which holds the same words.
        ("\nContinue? (y/n): ", "y"),
        (PowerShellPrompt, "if ($true) {\n\"yes\"\n}\n"),
        (PowerShellPrompt, "cls"),
        (PowerShellPrompt, "Write-Output after-cls"),
        (PowerShellPrompt, ""),
        (PowerShellPrompt, "exit"),
    ];

    private static readonly (string Trigger, string Command)[] CmdSends =
    [
        (CmdPrompt, "echo hello"),
        (CmdPrompt, "cmd /c exit 3"),
        (CmdPrompt, "dir /b"),
        (CmdPrompt, "set /p answer=Continue? (y/n): "),
        ("\nContinue? (y/n):", "y"),
        (CmdPrompt, "cls"),
        (CmdPrompt, "echo after-cls"),
        (CmdPrompt, ""),
        (CmdPrompt, "exit"),
    ];

    private static (string Trigger, string Command)[] SendsOf(string fixture) => fixture == PowerShell ? PowerShellSends : CmdSends;

    [Theory]
    [InlineData(PowerShell)]
    [InlineData(Cmd)]
    public void Transcript_matches_the_expected_file(string fixture)
    {
        var model = Replay(fixture, SendsOf(fixture));
        var actual = Render(model).Replace("\r\n", "\n").TrimEnd('\n');
        var path = Path.Combine(FixtureDirectory(), fixture + ".expected.txt");
        if (Environment.GetEnvironmentVariable("AXTERMINAL_UPDATE_EXPECTED") == "1")
        {
            File.WriteAllText(path, actual + "\n");
        }

        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd('\n'), actual);
    }

    [Theory]
    [InlineData(PowerShell)]
    [InlineData(Cmd)]
    public void Chunk_boundaries_do_not_change_the_transcript(string fixture)
    {
        var expected = Render(Replay(fixture, SendsOf(fixture)));
        for (var seed = 1; seed <= 5; seed++)
        {
            Assert.Equal(expected, Render(Replay(fixture, SendsOf(fixture), seed)));
        }
    }

    [Fact]
    public void PowerShell_marks_give_the_state()
    {
        var ready = 0;
        var finished = new List<int?>();
        var model = Replay(PowerShell, PowerShellSends, subscribe: m =>
        {
            m.Ready += () => ready++;
            m.CommandFinished += finished.Add;
        });

        Assert.Equal(1, ready);
        // hello, exit 3, the listing, Read-Host, the block, cls, after-cls; the bare Enter is no command and exit prints no prompt.
        Assert.Equal([0, 3, 0, 0, 0, 0, 0], finished);
        Assert.True(model.HasMarks);
        // exit was sent and no prompt came back: the window learns of the exit from the process and tells the model.
        Assert.True(model.Running);
        model.ShellExited(0);
        Assert.False(model.Running);
        Assert.Equal("System: The shell ended with code 0", model.Lines[^1].Text);
        Assert.Equal("PS C:\\axit-fixture>", model.Prompt);
        Assert.Equal("C:\\axit-fixture", model.Directory);
        Assert.Equal(9, model.ExchangeCount);
        var visible = model.Lines.Where(l => !l.Hidden).Select(l => l.Text).ToList();
        Assert.DoesNotContain(visible, t => t.StartsWith("PS C:\\axit-fixture>", StringComparison.Ordinal));
        Assert.DoesNotContain(visible, t => t.StartsWith(">>", StringComparison.Ordinal));
        Assert.Contains("hello", visible);
        Assert.Contains("Continue? (y/n): y", visible);
        Assert.Contains("yes", visible);
        Assert.Contains("after-cls", visible);
    }

    [Fact]
    public void Command_prompt_marks_give_the_state_without_codes()
    {
        var finished = new List<int?>();
        var model = Replay(Cmd, CmdSends, subscribe: m => m.CommandFinished += finished.Add);
        model.ShellExited(0);
        Assert.Equal([null, null, null, null, null, null], finished);
        Assert.False(model.Running);
        Assert.Equal("C:\\axit-fixture>", model.Prompt);
        Assert.Equal("C:\\axit-fixture", model.Directory);
        var visible = model.Lines.Where(l => !l.Hidden).Select(l => l.Text).ToList();
        Assert.DoesNotContain(visible, t => t.StartsWith("C:\\axit-fixture>", StringComparison.Ordinal));
        Assert.Contains("hello", visible);
        Assert.Contains("a.txt", visible);
        Assert.Contains("after-cls", visible);
    }
}
