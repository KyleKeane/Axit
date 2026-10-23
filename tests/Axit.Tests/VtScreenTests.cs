using Axit.Core.Transcript;
using Axit.Core.Vt;
using Screen = Axit.Core.Vt.Screen;

namespace Axit.Tests;

/// <summary>
/// The screen's rows and their lines (docs/axterminal/SPEC.md AT-4.1, AT-4.2): a line stays bound to its row as the
/// rows scroll, is found again by identity, and is committed when its row leaves the screen.
/// </summary>
public sealed class VtScreenTests
{
    private sealed class Store : LineStore
    {
        public override void AddSystemLine(string text)
        {
        }

        protected override void CompleteFrame()
        {
        }
    }

    [Fact]
    public void A_line_is_found_on_its_row_through_a_scroll_and_committed_when_it_leaves()
    {
        var screen = new Screen(20, 3, new Store());
        var parser = new VtParser(screen);
        parser.Feed("a\r\nb\r\n");
        var line = screen.EnsureLine(2);
        Assert.Equal(2, screen.RowOf(line));
        Assert.Equal(string.Empty, line.Text);

        parser.Feed("c\r\nd");
        screen.PullRowText();
        Assert.Equal(1, screen.RowOf(line));
        Assert.Equal("c", line.Text);
        Assert.False(line.Committed);

        parser.Feed("\r\ne\r\nf");
        Assert.Equal(-1, screen.RowOf(line));
        Assert.True(line.Committed);
        Assert.Equal("c", line.Text);
    }

    [Fact]
    public void Ensuring_a_row_gives_the_blank_rows_above_it_lines_too()
    {
        var store = new Store();
        var screen = new Screen(20, 4, store);
        new VtParser(screen).Feed("a\r\n\r\n");
        var line = screen.EnsureLine(2);
        Assert.Same(line, screen.LineAt(2));
        Assert.NotNull(screen.LineAt(1));
        Assert.Equal(3, store.Lines.Count);
        // Another screen's line is on no row of this one.
        Assert.Equal(-1, screen.RowOf(new Screen(20, 4, new Store()).EnsureLine(0)));
    }
}
