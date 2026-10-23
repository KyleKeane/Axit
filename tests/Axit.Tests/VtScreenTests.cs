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

    /// <summary>Detached rows start blank (docs/axterminal/SPEC.md AT-4.4): a shorter rewrite reads the new text alone, and a row left alone comes back as a blank line, not as a copy of the old one.</summary>
    [Fact]
    public void Detached_rows_keep_their_lines_text_and_start_blank()
    {
        var store = new Store();
        var screen = new Screen(20, 3, store);
        var parser = new VtParser(screen);
        parser.Feed("first long row\r\nsecond row");
        screen.PullRowText();
        var first = screen.LineAt(0)!;
        screen.DetachRows();
        Assert.True(first.Committed);
        Assert.Equal("first long row", first.Text);

        // The program homes the cursor and rewrites row 0 shorter, then prints on row 2: row 1 is a blank new line, not "second row" again.
        parser.Feed("\x1b[Hnew\r\n\r\nthird");
        screen.PullRowText();
        Assert.Equal("new", screen.LineAt(0)!.Text);
        Assert.Equal(string.Empty, screen.LineAt(1)!.Text);
        Assert.Equal("third", screen.LineAt(2)!.Text);
        Assert.Equal(5, store.Lines.Count);
    }

    /// <summary>A terminal reset lets the rows go like an erase: the Cleared event comes before the rows are blanked.</summary>
    [Fact]
    public void A_reset_raises_cleared_first()
    {
        var screen = new Screen(20, 3, new Store());
        var parser = new VtParser(screen);
        parser.Feed("kept");
        screen.PullRowText();
        var line = screen.LineAt(0)!;
        var seen = string.Empty;
        screen.Cleared += () => seen = screen.LineAt(0)?.Text ?? "(no line)";
        // ESC c (written as \u001b so that the c is not read as a hex digit of the escape).
        parser.Feed("\u001bc");
        Assert.Equal("kept", seen);
        Assert.Equal("kept", line.Text);
        Assert.True(screen.ClearedSinceFrameEnd);
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
