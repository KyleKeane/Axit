using Axit.Core;

namespace Axit.Tests;

/// <summary>The window's own undo and redo history (docs/axdown/SPEC.md AD-2.5).</summary>
public class EditHistoryTests
{
    /// <summary>A change not made by a typed key: a paste, a cut, a command, a reload.</summary>
    private const int NotTyped = -1;

    [Fact]
    public void The_step_between_two_texts_is_what_changed_between_the_common_prefix_and_suffix()
    {
        Assert.Equal(new EditStep(3, string.Empty, "XY"), EditHistory.Diff("abcdef", "abcXYdef", 5));
        Assert.Equal(new EditStep(2, "cd", string.Empty), EditHistory.Diff("abcdef", "abef", 2));
        Assert.Equal(new EditStep(1, "bcd", "Q"), EditHistory.Diff("abcdef", "aQef", 2));
        Assert.Equal(new EditStep(0, string.Empty, "abc"), EditHistory.Diff(string.Empty, "abc", 3));
        Assert.Equal(new EditStep(0, "abc", string.Empty), EditHistory.Diff("abc", string.Empty, 0));
        Assert.Null(EditHistory.Diff("same", "same", 2));
    }

    [Fact]
    public void An_ambiguous_place_is_the_one_the_caret_tells()
    {
        // "aa" from "a": typed at the end (caret 2) or at the start (caret 1).
        Assert.Equal(new EditStep(1, string.Empty, "a"), EditHistory.Diff("a", "aa", 2));
        Assert.Equal(new EditStep(0, string.Empty, "a"), EditHistory.Diff("a", "aa", 1));
        // "a" from "aa": Backspace at the end (caret 1) or Delete at the start (caret 0).
        Assert.Equal(new EditStep(1, "a", string.Empty), EditHistory.Diff("aa", "a", 1));
        Assert.Equal(new EditStep(0, "a", string.Empty), EditHistory.Diff("aa", "a", 0));
        // A line equal to its neighbour: typed at the end (caret 7), or "x" and Enter at the start of the second line (caret 6).
        Assert.Equal(new EditStep(4, string.Empty, "\r\nx"), EditHistory.Diff("x\r\nx", "x\r\nx\r\nx", 7));
        Assert.Equal(new EditStep(3, string.Empty, "x\r\n"), EditHistory.Diff("x\r\nx", "x\r\nx\r\nx", 6));
    }

    [Fact]
    public void A_step_undone_and_redone_gives_the_texts_back()
    {
        var step = EditHistory.Diff("abcdef", "aQef", 2)!;
        Assert.Equal("abcdef", step.Undo("aQef"));
        Assert.Equal("aQef", step.Redo("abcdef"));
    }

    [Fact]
    public void A_run_of_typing_is_one_step_and_a_break_starts_another()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "a", 1, typedAt: 0);
        history.Record("a", "ab", 2, typedAt: 1);
        history.Record("ab", "abc", 3, typedAt: 2);
        Assert.Equal(3, history.RunCaret);
        history.Break();
        Assert.Equal(-1, history.RunCaret);
        history.Record("abc", "abcd", 4, typedAt: 3);

        Assert.Equal(new EditStep(3, string.Empty, "d"), history.Undo());
        Assert.Equal(new EditStep(0, string.Empty, "abc"), history.Undo());
        Assert.Null(history.Undo());
    }

    [Fact]
    public void Typing_elsewhere_and_a_change_not_typed_are_steps_of_their_own()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "ab", 2, typedAt: 0);
        // Typed at the start, not at the run's caret.
        history.Record("ab", "Xab", 1, typedAt: 0);
        // Pasted at the run's caret: its own step, and no run for the typing after it to join.
        history.Record("Xab", "Xabc", 4, NotTyped);
        history.Record("Xabc", "Xabcd", 5, typedAt: 4);
        Assert.Equal(new EditStep(4, string.Empty, "d"), history.Undo());
        Assert.Equal(new EditStep(3, string.Empty, "c"), history.Undo());
        Assert.Equal(new EditStep(0, string.Empty, "X"), history.Undo());
        Assert.Equal(new EditStep(0, string.Empty, "ab"), history.Undo());
    }

    [Fact]
    public void Backspace_takes_the_run_back_and_then_reaches_into_the_text_before_it()
    {
        var history = new EditHistory();
        history.Record("xy", "xyab", 4, typedAt: 2);
        history.Record("xyab", "xya", 3, typedAt: 4);
        Assert.Equal(3, history.RunCaret);
        history.Record("xya", "xy", 2, typedAt: 3);
        history.Record("xy", "x", 1, typedAt: 2);
        Assert.Equal(1, history.RunCaret);

        Assert.Equal(new EditStep(1, "y", string.Empty), history.Undo());
        Assert.Null(history.Undo());
    }

    [Fact]
    public void A_run_of_backspaces_and_a_run_of_deletes_are_one_step_each()
    {
        var history = new EditHistory();
        history.Record("abcdef", "abcde", 5, typedAt: 6);
        history.Record("abcde", "abcd", 4, typedAt: 5);
        history.Record("abcd", "bcd", 0, typedAt: 0);
        history.Record("bcd", "cd", 0, typedAt: 0);
        Assert.Equal(0, history.RunCaret);

        Assert.Equal(new EditStep(0, "ab", string.Empty), history.Undo());
        Assert.Equal(new EditStep(4, "ef", string.Empty), history.Undo());
    }

    [Fact]
    public void A_character_typed_over_a_selection_is_a_step_that_typing_goes_on_from()
    {
        var history = new EditHistory();
        history.Record("abcdef", "aQef", 2, typedAt: 1);
        history.Record("aQef", "aQRef", 3, typedAt: 2);
        Assert.Equal(new EditStep(1, "bcd", "QR"), history.Undo());
    }

    [Fact]
    public void Undo_and_redo_walk_the_steps_and_a_new_change_drops_the_redo_side()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "one", 3, NotTyped);
        history.Record("one", "one two", 7, NotTyped);
        Assert.Null(history.Redo());

        var text = "one two";
        var step = history.Undo()!;
        text = step.Undo(text);
        Assert.Equal("one", text);
        Assert.True(history.CanRedo);
        step = history.Redo()!;
        text = step.Redo(text);
        Assert.Equal("one two", text);
        Assert.False(history.CanRedo);

        history.Undo();
        history.Record("one", "one 2", 5, NotTyped);
        Assert.False(history.CanRedo);
        Assert.Equal(new EditStep(3, string.Empty, " 2"), history.Undo());
        Assert.Equal(new EditStep(0, string.Empty, "one"), history.Undo());
        Assert.Null(history.Undo());
    }

    [Fact]
    public void An_undo_ends_the_open_run()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "ab", 2, typedAt: 0);
        history.Undo();
        history.Redo();
        Assert.Equal(-1, history.RunCaret);
        history.Record("ab", "abc", 3, typedAt: 2);
        Assert.Equal(new EditStep(2, string.Empty, "c"), history.Undo());
    }

    [Fact]
    public void The_size_counts_the_deltas_held_once_merges_and_drops_included()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "a", 1, 0);
        history.Record("a", "ab", 2, 1);
        history.Record("ab", "abc", 3, 2);
        // One run of typing: one step of three characters, not three steps.
        Assert.Equal(3, history.Size);
        history.Record("abc", "abc" + new string('x', 10), 13, NotTyped);
        Assert.Equal(13, history.Size);

        // An undone step still counts, on the redo side, until a new change drops it.
        Assert.NotNull(history.Undo());
        Assert.Equal(13, history.Size);
        history.Record("abc", "abcd", 4, 3);
        Assert.Equal(4, history.Size);

        history.Clear();
        Assert.Equal(0, history.Size);
    }

    [Fact]
    public void The_oldest_steps_are_dropped_beyond_the_limit()
    {
        var history = new EditHistory();
        var big = new string('x', EditHistory.Limit / 2 + 1);
        history.Record(string.Empty, "a", 1, NotTyped);
        history.Record("a", "a" + big, big.Length + 1, NotTyped);
        history.Record("a" + big, "a" + big + big, 2 * big.Length + 1, NotTyped);

        Assert.NotNull(history.Undo());
        Assert.Null(history.Undo());
    }

    [Fact]
    public void Clear_forgets_everything()
    {
        var history = new EditHistory();
        history.Record(string.Empty, "a", 1, NotTyped);
        history.Undo();
        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Null(history.Redo());
    }
}
