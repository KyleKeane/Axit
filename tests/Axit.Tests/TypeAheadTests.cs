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
}
