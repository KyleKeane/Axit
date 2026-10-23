using Axit.Core.Transcript;
using Axit.Core.Vt;
using Screen = Axit.Core.Vt.Screen;

namespace Axit.Tests;

/// <summary>
/// The screen's operations, one fact each (docs/axclaude/SPEC.md §7.5, docs/axterminal/SPEC.md AT-4): the scroll
/// region, inserted and deleted lines, the kinds of erase, a wide character at the last column, a combining mark, a
/// home without an erase. The line identity across them is <see cref="VtScreenTests"/>.
/// </summary>
public sealed class VtScreenOperationTests
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

    /// <summary>A store with a screen of its own, for feeding bytes: the UTF-8 decoder is the store's.</summary>
    private sealed class FedStore : LineStore
    {
        public FedStore(int columns, int rows)
            : base(columns, rows)
        {
            Screen = NewScreen();
        }

        public Screen Screen { get; }

        public override void AddSystemLine(string text)
        {
        }

        protected override void CompleteFrame()
        {
        }
    }

    [Fact]
    public void A_utf8_sequence_split_across_chunks_is_one_character()
    {
        var store = new FedStore(20, 2);
        // é is two bytes, 漢 three: the chunks cut both sequences.
        var bytes = "é漢x"u8.ToArray();
        store.Feed(bytes.AsSpan(0, 1));
        store.Feed(bytes.AsSpan(1, 2));
        store.Feed(bytes.AsSpan(3));
        store.Screen.PullRowText();
        Assert.Equal("é漢x", store.Screen.LineAt(0)!.Text);
    }

    private static (Screen Screen, VtParser Parser) Make(int columns = 20, int rows = 4)
    {
        var screen = new Screen(columns, rows, new Store());
        return (screen, new VtParser(screen));
    }

    /// <summary>The rows' texts as the lines read them, a row without a line as empty.</summary>
    private static string[] Rows(Screen screen)
    {
        screen.PullRowText();
        return Enumerable.Range(0, screen.Rows).Select(row => screen.LineAt(row)?.Text ?? string.Empty).ToArray();
    }

    [Fact]
    public void A_scroll_region_scrolls_its_rows_alone_and_drops_what_leaves_it()
    {
        var store = new Store();
        var screen = new Screen(20, 4, store);
        var parser = new VtParser(screen);
        parser.Feed("a\r\nb\r\nc\r\nd");
        screen.PullRowText();
        var b = screen.LineAt(1)!;
        // Rows 2 to 4 are the region; a line feed on its last row scrolls the region, and the first row stays.
        parser.Feed("\x1b[2;4r\x1b[4;1H\r\ne");
        Assert.Equal(["a", "c", "d", "e"], Rows(screen));
        // A line scrolled out of a region inside the screen is a pane's content, not the transcript's history: it is
        // dropped from the store, not committed; only a scroll from the top row commits (VtScreenTests).
        Assert.Equal(-1, screen.RowOf(b));
        Assert.False(b.Committed);
        Assert.DoesNotContain(b, store.Lines);
    }

    [Fact]
    public void Lines_are_inserted_and_deleted_at_the_cursor_row()
    {
        var (screen, parser) = Make();
        parser.Feed("a\r\nb\r\nc");
        parser.Feed("\x1b[2;1H\x1b[L");
        Assert.Equal(["a", "", "b", "c"], Rows(screen));
        parser.Feed("\x1b[M");
        Assert.Equal(["a", "b", "c", ""], Rows(screen));
    }

    [Fact]
    public void The_erases_clear_to_the_end_of_the_line_to_its_start_the_whole_line_and_the_screen()
    {
        var (screen, parser) = Make();
        parser.Feed("hello world");
        parser.Feed("\x1b[7G\x1b[K");
        Assert.Equal("hello", Rows(screen)[0].TrimEnd());

        parser.Feed("\x1b[2;1Habcdef\x1b[3G\x1b[1K");
        var second = Rows(screen)[1];
        Assert.EndsWith("def", second);
        Assert.DoesNotContain("a", second);
        parser.Feed("\x1b[2K");
        Assert.Equal(string.Empty, Rows(screen)[1].Trim());

        parser.Feed("\x1b[3;1Hthird\r\nfourth");
        var cleared = false;
        screen.Cleared += () => cleared = true;
        parser.Feed("\x1b[2J");
        Assert.True(cleared);
        Assert.All(Rows(screen), row => Assert.Equal(string.Empty, row.Trim()));
    }

    [Fact]
    public void A_wide_character_that_does_not_fit_the_last_column_wraps_whole()
    {
        var (screen, parser) = Make(columns: 5);
        parser.Feed("abcd漢");
        var rows = Rows(screen);
        Assert.Equal("abcd", rows[0].TrimEnd());
        Assert.StartsWith("漢", rows[1]);
    }

    [Fact]
    public void A_combining_mark_joins_its_cell_and_takes_no_column()
    {
        var (screen, parser) = Make(columns: 3);
        parser.Feed("éxy");
        var rows = Rows(screen);
        Assert.Equal("éxy", rows[0]);
        Assert.Equal(string.Empty, rows[1]);
    }

    [Fact]
    public void A_home_without_an_erase_writes_over_the_first_row_and_says_so()
    {
        var (screen, parser) = Make();
        parser.Feed("abc\r\ndef");
        var homed = 0;
        screen.Homed += () => homed++;
        parser.Feed("\x1b[HX");
        Assert.Equal(1, homed);
        Assert.Equal(["Xbc", "def", "", ""], Rows(screen));
    }
}
