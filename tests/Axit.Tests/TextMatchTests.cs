using Axit.Core;

namespace Axit.Tests;

/// <summary>The shared matching of a typed text against a name (docs/axit/SPEC.md AX-10.3, AxStart AS-2.2).</summary>
public sealed class TextMatchTests
{
    private static int? Rank(string name, string text) => TextMatch.Rank(TextMatch.Fold(name), TextMatch.Words(text));

    [Fact]
    public void A_name_starting_with_the_first_word_ranks_before_a_word_and_a_word_before_the_rest()
    {
        Assert.Equal(0, Rank("Notepad", "note"));
        Assert.Equal(1, Rank("Sticky Notes", "note"));
        Assert.Equal(2, Rank("OneNote", "note"));
    }

    [Fact]
    public void Every_word_must_be_in_the_name_in_any_order()
    {
        Assert.Equal(1, Rank("Larger text, Options, Text", "text larger"));
        Assert.Null(Rank("Larger text, Options, Text", "text smaller"));
    }

    [Fact]
    public void Case_and_accents_are_ignored()
    {
        Assert.Equal(0, Rank("Café Menu", "CAFE"));
        Assert.Equal(0, Rank("Cafe Menu", "café"));
        Assert.Equal(0, Rank("Łódź", "lodz"));
        Assert.Equal(0, Rank("Ångström", "ANGSTROM"));
        Assert.Equal("uber", TextMatch.Fold("Über"));
        Assert.Equal("cafe", TextMatch.Fold("café"));
        Assert.Equal(0, Rank("Программа", "ПРОГРАММА"));
    }

    /// <summary>The tests run without the globalization data, as the executable does (tests/Directory.Build.props), so that the folding above is the table's and not the runtime's.</summary>
    [Fact]
    public void The_tests_run_in_invariant_globalization_mode_like_the_program()
    {
        Assert.True(AppContext.TryGetSwitch("System.Globalization.Invariant", out var invariant) && invariant);
    }

    [Fact]
    public void An_empty_text_matches_everything_first()
    {
        Assert.Equal(0, Rank("Anything", "   "));
        Assert.Empty(TextMatch.Words("  "));
    }
}
