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
    public void Ctrl_Up_goes_back_through_the_commands_and_Ctrl_Down_comes_forward_to_the_draft()
    {
        var history = new CommandHistory();
        history.Add("dir");
        history.Add("git status");

        Assert.Null(history.Newer("half typed"));
        Assert.Equal("git status", history.Older("half typed"));
        Assert.Equal("dir", history.Older("git status"));
        Assert.Null(history.Older("dir"));
        Assert.Equal("git status", history.Newer("dir"));
        Assert.Equal("half typed", history.Newer("git status"));
        Assert.Null(history.Newer("half typed"));

        // A send starts afresh from the newest, with an empty draft.
        history.Older("half typed");
        history.Add("ls");
        Assert.Equal("ls", history.Older(""));
        Assert.Equal("", history.Newer("ls"));
    }

    [Fact]
    public void A_command_changed_and_left_becomes_the_last_item_and_the_command_stays_as_it_was()
    {
        var history = new CommandHistory();
        history.Add("dir");
        history.Add("git status");

        Assert.Equal("git status", history.Older("draft"));
        Assert.Equal("dir", history.Older("git status --short"));
        Assert.Equal("git status", history.Newer("dir"));
        Assert.Equal("git status --short", history.Newer("git status"));
        Assert.Equal(["git status", "dir"], history.Items);
    }
}
