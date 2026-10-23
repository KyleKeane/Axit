using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The marks on a folder's entries (docs/axfile/SPEC.md AF-3.7, AF-9.2): each key's change, its line and its words.</summary>
public sealed class MarksTests
{
    private static readonly IReadOnlyList<Entry> Shown = Entries("a.txt", "b.txt", "c.txt", "d.txt", "e.txt");

    private static IReadOnlyList<Entry> Entries(params string[] names) =>
        names.Select(name => new Entry(name, "C:\\x\\" + name, false, 0, DateTime.Now)).ToList();

    [Fact]
    public void Space_marks_and_unmarks_the_entry_with_the_count_when_more_are_marked()
    {
        var marks = new Marks();
        var first = marks.Toggle(Shown, 1);
        Assert.Equal([1], first.Changed);
        Assert.Empty(first.Before);
        Assert.Equal("Selected b.txt", first.Line);
        Assert.Equal("b.txt", first.Select);
        Assert.Equal("Selected", first.Said);
        Assert.True(marks.Contains("C:\\x\\b.txt"));

        var second = marks.Toggle(Shown, 3);
        Assert.Equal("Selected, 2 selected", second.Said);
        Assert.Equal(["C:\\x\\b.txt"], second.Before);

        var off = marks.Toggle(Shown, 1);
        Assert.Equal("Deselected b.txt", off.Line);
        Assert.Equal("Deselected, 1 selected", off.Said);
        Assert.Equal(1, marks.Count);
        Assert.Equal("Deselected", marks.Toggle(Shown, 3).Said);
        Assert.Equal(0, marks.Count);
    }

    [Fact]
    public void Select_all_and_deselect_all_record_a_line_only_when_something_changed()
    {
        var marks = new Marks();
        var all = marks.All(Shown, 2);
        Assert.Equal([0, 1, 2, 3, 4], all.Changed);
        Assert.Equal("Selected all, 5 items", all.Line);
        Assert.Equal("c.txt", all.Select);
        Assert.Equal("5 selected", all.Said);

        var again = marks.All(Shown, 2);
        Assert.Empty(again.Changed);
        Assert.Null(again.Line);
        Assert.Equal("5 selected", again.Said);

        var none = marks.None(Shown, 0);
        Assert.Equal([0, 1, 2, 3, 4], none.Changed);
        Assert.Equal("Deselected all, 5 items", none.Line);
        Assert.Equal(5, none.Before.Count);
        Assert.Equal("None selected", none.Said);
        Assert.Null(marks.None(Shown, 0).Line);
    }

    [Fact]
    public void Shift_space_marks_from_the_nearest_marked_entry_and_is_space_without_one()
    {
        var marks = new Marks();
        marks.Toggle(Shown, 1);
        var toHere = marks.ToHere(Shown, 4);
        Assert.Equal([2, 3, 4], toHere.Changed);
        Assert.Equal("Selected 4 items", toHere.Line);
        Assert.Equal("e.txt", toHere.Select);
        Assert.Equal("Selected, 4 selected", toHere.Said);

        // Both as near: the one above.
        var even = new Marks();
        even.Toggle(Shown, 0);
        even.Toggle(Shown, 4);
        Assert.Equal([1, 2], even.ToHere(Shown, 2).Changed);

        // No other entry marked: Space, so on the one marked entry it unmarks it again (decided 2026-10-23).
        var alone = new Marks();
        Assert.Equal("Selected", alone.ToHere(Shown, 2).Said);
        Assert.Equal("Deselected", alone.ToHere(Shown, 2).Said);
        Assert.Equal(0, alone.Count);
    }

    [Fact]
    public void A_shift_range_begins_at_the_current_entry_keeps_the_marks_before_it_and_shrinks_back()
    {
        var marks = new Marks();
        marks.Toggle(Shown, 0);
        var begin = marks.Extend(Shown, 2, 3);
        Assert.True(begin.RangeBegins);
        Assert.Equal([2, 3], begin.Changed);
        Assert.Equal(["C:\\x\\a.txt"], begin.Before);
        Assert.Equal("Selected 2 items", begin.Line);
        Assert.Equal("d.txt", begin.Select);
        Assert.Equal(string.Empty, begin.Said);
        Assert.True(marks.RangeOn);

        var on = marks.Extend(Shown, 3, 4);
        Assert.False(on.RangeBegins);
        Assert.Equal([4], on.Changed);
        Assert.Equal("Selected 3 items", on.Line);

        var back = marks.Extend(Shown, 4, 2);
        Assert.False(back.RangeBegins);
        Assert.Equal([3, 4], back.Changed);
        Assert.Equal("Selected 1 item", back.Line);
        Assert.Equal(2, marks.Count);

        // The current entry moved without Shift: the next Shift key begins a new range from there, over the marks there are.
        var fresh = marks.Extend(Shown, 1, 0);
        Assert.True(fresh.RangeBegins);
        Assert.Equal([1], fresh.Changed);
        Assert.Equal(2, fresh.Before.Count);
        Assert.Equal(3, marks.Count);

        // Space ends the range.
        marks.Toggle(Shown, 4);
        Assert.False(marks.RangeOn);
    }

    [Fact]
    public void The_marks_on_entries_the_list_no_longer_shows_are_gone_and_the_range_ends()
    {
        var marks = new Marks();
        marks.All(Shown, 0);
        marks.Extend(Shown, 0, 1);
        var kept = marks.Keep(Entries("a.txt", "c.txt"));
        Assert.Equal(2, marks.Count);
        Assert.False(marks.Contains("C:\\x\\b.txt"));
        Assert.False(marks.RangeOn);
        Assert.Null(kept.Line);
        Assert.Equal(string.Empty, kept.Said);
        Assert.Equal(5, kept.Before.Count);

        marks.Clear();
        Assert.Equal(0, marks.Count);
    }

    [Fact]
    public void Undo_puts_the_marks_back_among_the_entries_shown()
    {
        var marks = new Marks();
        marks.Toggle(Shown, 0);
        var all = marks.All(Shown, 0);
        var restored = marks.Restore(all.Before, Shown);
        Assert.Equal([1, 2, 3, 4], restored.Changed);
        Assert.Equal("Undone: 1 selected", restored.Said);
        Assert.Null(restored.Line);
        Assert.Equal("Undone: none selected", marks.Restore([], Shown).Said);

        // An entry gone since is skipped.
        Assert.Equal("Undone: 1 selected", marks.Restore(["C:\\x\\zz.txt", "C:\\x\\e.txt"], Shown).Said);
        Assert.True(marks.Contains("C:\\x\\e.txt"));
    }
}
