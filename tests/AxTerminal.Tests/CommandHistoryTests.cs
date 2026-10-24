using AxTerminal.Core;

namespace AxTerminal.Tests;

public sealed class CommandHistoryTests
{
    [Fact]
    public void Newest_first_each_once_and_capped()
    {
        var history = new CommandHistory();
        history.Add("dir");
        history.Add("git status");
        history.Add(" dir ");
        history.Add("   ");
        Assert.Equal(["dir", "git status"], history.Items);

        for (var i = 0; i < CommandHistory.Limit + 5; i++)
        {
            history.Add($"echo {i}");
        }

        Assert.Equal(CommandHistory.Limit, history.Items.Count);
        Assert.Equal($"echo {CommandHistory.Limit + 4}", history.Items[0]);
        history.Clear();
        Assert.Empty(history.Items);
    }

    [Fact]
    public void Up_goes_back_through_the_commands_and_Down_comes_forward_to_the_draft()
    {
        var history = new CommandHistory();
        history.Add("dir");
        history.Add("git status");

        Assert.Null(history.Newer());
        Assert.Equal("git status", history.Older("half typed"));
        Assert.Equal("dir", history.Older("git status"));
        Assert.Null(history.Older("dir"));
        Assert.Equal("git status", history.Newer());
        Assert.Equal("half typed", history.Newer());
        Assert.Null(history.Newer());

        // A send starts afresh from the newest.
        history.Older("");
        history.Add("ls");
        Assert.Equal("ls", history.Older(""));
        history.Reset();
        Assert.Equal("ls", history.Older("other"));
    }
}
