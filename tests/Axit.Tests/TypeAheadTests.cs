using Axit.Core;

namespace Axit.Tests;

/// <summary>Where letters typed in a list jump (docs/axfile/SPEC.md AF-3.6); from AxFile.Tests with the function, 2026-10-24.</summary>
public sealed class TypeAheadTests
{
    [Fact]
    public void Typing_builds_a_prefix_and_a_run_of_one_letter_cycles()
    {
        IReadOnlyList<string> shown = ["aardvark", "apple", "apricot", "banana"];
        // One letter: the next name starting with it, round the end.
        Assert.Equal(1, TypeAhead.Find(shown, "a", 0));
        Assert.Equal(0, TypeAhead.Find(shown, "A", 3));
        // The letter again: the cycle goes on.
        Assert.Equal(2, TypeAhead.Find(shown, "aa", 1));
        // Another letter: the prefix whole, from the entry in hand.
        Assert.Equal(1, TypeAhead.Find(shown, "ap", 1));
        Assert.Equal(2, TypeAhead.Find(shown, "apr", 1));
        Assert.Equal(0, TypeAhead.Find(shown, "aar", 2));
        Assert.Equal(-1, TypeAhead.Find(shown, "z", 0));
        Assert.Equal(-1, TypeAhead.Find([], "a", 0));
    }

    [Fact]
    public void Shift_searches_the_same_way_backwards()
    {
        IReadOnlyList<string> shown = ["aardvark", "apple", "apricot", "banana"];
        // One letter backwards: the name before that starts with it, round the start.
        Assert.Equal(1, TypeAhead.Find(shown, "A", 2, backward: true));
        Assert.Equal(2, TypeAhead.Find(shown, "a", 0, backward: true));
        Assert.Equal(3, TypeAhead.Find(shown, "B", 0, backward: true));
        // A prefix backwards: from the item in hand up to the first, then round the end.
        Assert.Equal(2, TypeAhead.Find(shown, "Apr", 2, backward: true));
        Assert.Equal(2, TypeAhead.Find(shown, "ap", 3, backward: true));
        Assert.Equal(3, TypeAhead.Find(shown, "ban", 0, backward: true));
        Assert.Equal(-1, TypeAhead.Find(shown, "Z", 1, backward: true));
        // Capitals typed forwards find the same names: the case never matters.
        Assert.Equal(TypeAhead.Find(shown, "ap", 0), TypeAhead.Find(shown, "AP", 0));
    }
}