using Axit.Core;

namespace Axit.Tests;

/// <summary>Links, lists and their items, block quotes and tables for the navigation keys (docs/axdown/SPEC.md §9): what counts, what is skipped, what is spoken, next and previous.</summary>
public sealed class MarkdownElementsTests
{
    private static readonly string[] LinesOfText =
    [
        "Intro with [GitHub](https://github.com) and <https://x.org> and https://bare.example/path.",   // 0
        "",                                                                                             // 1
        "- one",                                                                                        // 2
        "- two",                                                                                        // 3
        "  - nested",                                                                                   // 4
        "- three",                                                                                      // 5
        "",                                                                                             // 6
        "1. first",                                                                                     // 7
        "2. second",                                                                                    // 8
        "",                                                                                             // 9
        "> quoted",                                                                                     // 10
        "> more",                                                                                       // 11
        "",                                                                                             // 12
        "| A | B |",                                                                                    // 13
        "|---|:-:|",                                                                                    // 14
        "| 1 | 2 |",                                                                                    // 15
        "| 3 | 4 |",                                                                                    // 16
        "",                                                                                             // 17
        "```",                                                                                          // 18
        "- not an item",                                                                                // 19
        "[not](a-link)",                                                                                // 20
        "> not a quote",                                                                                // 21
        "```",                                                                                          // 22
        "Tail `[code](x)` text and ![an image](pic.png) only.",                                         // 23
    ];

    private static readonly string Text = string.Join("\r\n", LinesOfText);

    /// <summary>The offset of a line's first character.</summary>
    private static int Line(int number) => LinesOfText.Take(number).Sum(l => l.Length + 2);

    [Fact]
    public void Links_are_inline_reference_bracketed_and_bare_outside_code()
    {
        var links = MarkdownOutline.Links(Text);
        Assert.Equal(
        [
            (11, "GitHub", "https://github.com"),
            (44, "https://x.org", "https://x.org"),
            (64, "https://bare.example/path", "https://bare.example/path"),
        ], links.Select(l => (l.Start, l.Text, l.Target)).ToList());
        Assert.Equal("GitHub, link", links[0].Spoken);
        Assert.Equal(Text.IndexOf(" and <", StringComparison.Ordinal), links[0].End);
    }

    [Fact]
    public void Next_link_starts_after_the_caret_and_previous_ends_at_or_before_it()
    {
        Assert.Equal(11, MarkdownOutline.NextLink(Text, 0)!.Start);
        Assert.Equal(44, MarkdownOutline.NextLink(Text, 11)!.Start);     // from the start of the first link
        Assert.Equal(44, MarkdownOutline.NextLink(Text, 20)!.Start);     // from inside it
        Assert.Null(MarkdownOutline.NextLink(Text, 64));
        Assert.Null(MarkdownOutline.PreviousLink(Text, 11));
        Assert.Null(MarkdownOutline.PreviousLink(Text, 20));             // inside the first link: nothing before it
        Assert.Equal(11, MarkdownOutline.PreviousLink(Text, 44)!.Start); // at the start of the second
        Assert.Equal(64, MarkdownOutline.PreviousLink(Text, Text.Length)!.Start);
    }

    [Fact]
    public void List_items_are_bullets_and_numbers_at_any_indentation_outside_code()
    {
        var items = MarkdownOutline.ListItems(Text);
        Assert.Equal(
        [
            (Line(2), 0, "-", "one"),
            (Line(3), 0, "-", "two"),
            (Line(4), 2, "-", "nested"),
            (Line(5), 0, "-", "three"),
            (Line(7), 0, "1.", "first"),
            (Line(8), 0, "2.", "second"),
        ], items.Select(i => (i.Start, i.Indent, i.Marker, i.Text)).ToList());
        Assert.Equal("nested", items[2].Spoken);
        Assert.Empty(MarkdownOutline.ListItems("---\r\n***\r\n+1 vote\r\n1.5 things\r\n-x"));
        Assert.Equal("blank", MarkdownOutline.ListItems("-")[0].Spoken);
    }

    [Fact]
    public void A_list_runs_through_its_items_nested_items_and_blank_lines_and_a_new_marker_kind_starts_another()
    {
        var lists = MarkdownOutline.Lists(Text);
        Assert.Equal([(Line(2), 3), (Line(7), 2)], lists.Select(l => (l.Start, l.Items.Count)).ToList());
        Assert.Equal("List with 3 items, one", lists[0].Spoken);

        var loose = MarkdownOutline.Lists("- a\r\n\r\n- b\r\n\r\n  more of b\r\n\r\ntext\r\n* c\r\n* d");
        Assert.Equal([2, 2], loose.Select(l => l.Items.Count).ToList());
        Assert.Equal("List with 2 items, c", loose[1].Spoken);
        Assert.Equal("List with 1 item, only", MarkdownOutline.Lists("- only")[0].Spoken);
    }

    [Fact]
    public void Block_quotes_and_tables_are_found_with_their_sizes()
    {
        var quotes = MarkdownOutline.BlockQuotes(Text);
        Assert.Equal([(Line(10), "quoted")], quotes.Select(q => (q.Start, q.Text)).ToList());
        Assert.Equal("Block quote, quoted", quotes[0].Spoken);
        Assert.Equal("deep", MarkdownOutline.BlockQuotes("> > deep")[0].Text);
        Assert.Equal("Block quote", MarkdownOutline.BlockQuotes(">")[0].Spoken);

        var tables = MarkdownOutline.Tables(Text);
        Assert.Equal([(Line(13), 3, 2)], tables.Select(t => (t.Start, t.Rows, t.Columns)).ToList());
        Assert.Equal("Table with 3 rows and 2 columns", tables[0].Spoken);
        Assert.Empty(MarkdownOutline.Tables("| a | b |\r\n| 1 | 2 |"));   // no delimiter row
        Assert.Empty(MarkdownOutline.Tables("a\r\n-"));                 // a setext heading, not a table
    }

    [Fact]
    public void Block_kinds_jump_by_line_so_a_jump_from_inside_never_lands_on_itself()
    {
        Assert.Equal(Line(2), MarkdownOutline.NextList(Text, 0)!.Start);
        Assert.Equal(Line(7), MarkdownOutline.NextList(Text, Line(3))!.Start);
        Assert.Equal(Line(2), MarkdownOutline.PreviousList(Text, Line(3))!.Start);
        Assert.Equal(Line(7), MarkdownOutline.PreviousList(Text, Line(8) + 3)!.Start);
        Assert.Null(MarkdownOutline.NextList(Text, Line(8)));

        Assert.Equal(Line(4), MarkdownOutline.NextListItem(Text, Line(3))!.Start);
        Assert.Equal(Line(8), MarkdownOutline.PreviousListItem(Text, Text.Length)!.Start);

        Assert.Equal(Line(10), MarkdownOutline.NextBlockQuote(Text, 0)!.Start);
        Assert.Null(MarkdownOutline.NextBlockQuote(Text, Line(11)));
        Assert.Equal(Line(10), MarkdownOutline.PreviousBlockQuote(Text, Line(11))!.Start);

        Assert.Equal(Line(13), MarkdownOutline.NextTable(Text, 0)!.Start);
        Assert.Null(MarkdownOutline.NextTable(Text, Line(13)));
        Assert.Equal(Line(13), MarkdownOutline.PreviousTable(Text, Line(16))!.Start);
    }
}
