using System.Text;
using AxClaude.Core.Transcript;
using Axit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>The joining rule behind every multi-line notification (D32): the spoken replies and the keys that read a kind of line to the end.</summary>
public class UtteranceTests
{
    [Fact]
    public void Lines_join_with_a_full_stop_unless_one_ends_with_a_pause_and_wrapped_rows_with_a_space()
    {
        var text = new StringBuilder();
        Utterance.Append(text, "claude: What Blue Is", joinedToPrevious: false);
        Utterance.Append(text, "Blue is a primary color.", joinedToPrevious: false);
        Utterance.Append(text, "- It is calm,", joinedToPrevious: false);
        Utterance.Append(text, "and it is trusted", joinedToPrevious: false);
        Utterance.Append(text, "by many", joinedToPrevious: true);
        Utterance.Append(text, " people.", joinedToPrevious: true);
        Assert.Equal("claude: What Blue Is. Blue is a primary color. - It is calm, and it is trusted by many people.", text.ToString());
    }
}
