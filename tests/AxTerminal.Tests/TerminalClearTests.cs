using AxTerminal.Core;
using static AxTerminal.Tests.TestHelpers;

namespace AxTerminal.Tests;

/// <summary>Clear transcript while a command runs (docs/axterminal/SPEC.md AT-3.4, AT-1.4): the command goes on, and what it prints next is seen.</summary>
public sealed class TerminalClearTests
{
    private static string Prompt(string? code) =>
        $"{Esc}]133;D{(code is null ? string.Empty : ";" + code)}{Bel}{Esc}]133;A{Bel}{Esc}]9;9;\"C:\\src\"{Bel}PS C:\\src> {Esc}]133;B{Bel}";

    [Fact]
    public void A_clear_during_a_running_command_keeps_it_running_and_shows_what_comes_next()
    {
        var model = new TerminalModel(80, 10);
        var finished = new List<int?>();
        model.CommandFinished += finished.Add;
        Feed(model, Prompt(null));
        model.EndFrame();
        Assert.True(model.Send("ping"));
        Feed(model, "ping\r\nfirst reply\r\n");
        model.EndFrame();
        Assert.True(model.Running);

        model.Clear();
        Assert.True(model.Running);
        Assert.Equal("ping", model.RunningCommand);
        Assert.DoesNotContain(model.Lines, line => !line.Hidden);

        Feed(model, $"second reply\r\n{Prompt("0")}");
        model.EndFrame();
        Assert.False(model.Running);
        Assert.Equal([0], finished);
        var visible = model.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToList();
        Assert.Contains("second reply", visible);
        Assert.DoesNotContain("first reply", visible);
        Assert.DoesNotContain(visible, text => text.StartsWith("PS C:", StringComparison.Ordinal));
    }
}
