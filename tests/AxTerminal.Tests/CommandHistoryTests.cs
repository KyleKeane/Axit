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
}
