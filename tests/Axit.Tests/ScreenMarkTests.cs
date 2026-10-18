using Axit.Core.Transcript;
using Axit.Core.Vt;
using Screen = Axit.Core.Vt.Screen;

namespace Axit.Tests;

/// <summary>
/// The screen reports a shell's integration marks and its current directory as events (docs/axterminal/SPEC.md
/// AT-1.3): OSC 133 with its letter and what follows, OSC 9;9 with the path, quotes removed. A title OSC still
/// raises the title event, and the text around the sequences lands on the rows as before.
/// </summary>
public sealed class ScreenMarkTests
{
    private const string Esc = "\x1b";

    private sealed class Store : LineStore
    {
    }

    [Fact]
    public void Marks_and_directory_are_reported()
    {
        var screen = new Screen(40, 5, new Store());
        var marks = new List<(char Letter, string? Code)>();
        var directories = new List<string>();
        var titles = new List<string>();
        screen.Mark += (letter, rest) => marks.Add((letter, rest));
        screen.DirectoryChanged += directories.Add;
        screen.TitleChanged += titles.Add;
        var parser = new VtParser(screen);
        parser.Feed($"{Esc}]133;D;0\a{Esc}]133;A\a{Esc}]9;9;\"C:\\src\\demo\"\a{Esc}]0;demo\aPS C:\\src\\demo> {Esc}]133;B{Esc}\\");
        screen.PullRowText();

        Assert.Equal([('D', "0"), ('A', null), ('B', null)], marks);
        Assert.Equal(["C:\\src\\demo"], directories);
        Assert.Equal(["demo"], titles);
        Assert.Equal("PS C:\\src\\demo>", screen.LineAt(0)?.Text);
    }

    [Fact]
    public void Other_oscs_are_ignored()
    {
        var screen = new Screen(40, 5, new Store());
        var count = 0;
        screen.Mark += (_, _) => count++;
        screen.DirectoryChanged += _ => count++;
        new VtParser(screen).Feed($"{Esc}]9;4;1;50\a{Esc}]133\a{Esc}]7;file:///C:/x\a");

        Assert.Equal(0, count);
    }
}
