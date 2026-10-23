using Axit.Forms;

namespace Axit.Tests;

/// <summary>The text form of the hand-over between a starting process and an app's open window (docs/axmedia/SPEC.md AM-1.3).</summary>
public sealed class AppMessageTests
{
    [Fact]
    public void The_verb_and_the_paths_go_round_as_lines()
    {
        var text = AppMessage.Format(AppMessage.Play, [@"C:\Music\Dune\01 One.mp3", @"C:\Music\Dune\02 Two.mp3"]);
        Assert.Equal("play\nC:\\Music\\Dune\\01 One.mp3\nC:\\Music\\Dune\\02 Two.mp3", text);
        var message = AppMessage.Parse(text)!;
        Assert.Equal(AppMessage.Play, message.Verb);
        Assert.Equal([@"C:\Music\Dune\01 One.mp3", @"C:\Music\Dune\02 Two.mp3"], message.Paths);
    }

    [Fact]
    public void A_message_may_have_no_paths_and_tolerates_line_ends_and_the_terminator()
    {
        var add = AppMessage.Parse("add")!;
        Assert.Equal(AppMessage.Add, add.Verb);
        Assert.Empty(add.Paths);
        var open = AppMessage.Parse("open\r\nC:\\notes.md\r\n\0")!;
        Assert.Equal(AppMessage.Open, open.Verb);
        Assert.Equal([@"C:\notes.md"], open.Paths);
        Assert.Null(AppMessage.Parse(string.Empty));
        Assert.Null(AppMessage.Parse("\n\nC:\\x"));
    }

    [Fact]
    public void A_play_or_an_open_takes_the_focus_and_an_add_does_not()
    {
        Assert.True(AppMessage.TakesFocus(AppMessage.Play));
        Assert.True(AppMessage.TakesFocus(AppMessage.Open));
        Assert.False(AppMessage.TakesFocus(AppMessage.Add));
    }

    [Fact]
    public void Nothing_is_sent_to_an_app_that_is_not_in_the_bundle_or_has_no_window()
    {
        Assert.False(AppMessage.Send("AxNothing", AppMessage.Open, [@"C:\x"]));
        // No axPreview window is open while the tests run.
        Assert.False(AppMessage.Send("axPreview", AppMessage.Open, [@"C:\x"]));
    }
}
