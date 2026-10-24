using AxList.Core;

namespace AxList.Tests;

/// <summary>A list file read, shown and changed (docs/axlist/SPEC.md §5, LI-4): every line kept that nothing changed.</summary>
public sealed class ListDocumentTests
{
    private const string Shopping = "# Shopping\r\n\r\n- [ ] Milk\r\n- [x] Eggs\r\n  - [ ] Free range\r\n* Butter\r\n1. First\r\n2) Second\r\nA note under the list\r\n";

    private static ListDocument Parse(string text) => ListDocument.Parse(text);

    [Fact]
    public void A_file_nothing_changed_is_written_back_byte_for_byte()
    {
        foreach (var text in new[] { Shopping, "- one\r\n-   two spaced\r\n\t- tabbed\r\n---\r\n#hashtag\r\n1.5 kg flour", string.Empty, "\r\n\r\n" })
        {
            Assert.Equal(text, Parse(text).ToText());
        }
    }

    [Fact]
    public void Each_line_is_read_as_its_kind()
    {
        var lines = Parse(Shopping + "---\r\n#tag\r\n- \r\n- [x]\r\n").Lines;
        Assert.Equal(LineKind.Heading, lines[0].Kind);
        Assert.Equal("Shopping", lines[0].Text);
        Assert.Equal(LineKind.Blank, lines[1].Kind);
        Assert.True(lines[2] is { Kind: LineKind.Item, IsTask: true, Done: false, Text: "Milk", Marker: "-", Width: 0 });
        Assert.True(lines[3] is { IsTask: true, Done: true, Text: "Eggs" });
        Assert.True(lines[4] is { IsTask: true, Width: 2, Text: "Free range" });
        Assert.True(lines[5] is { Kind: LineKind.Item, IsTask: false, Marker: "*", Text: "Butter" });
        Assert.True(lines[6] is { IsNumbered: true, Number: 1, Marker: ".", Text: "First" });
        Assert.True(lines[7] is { IsNumbered: true, Number: 2, Marker: ")", Text: "Second" });
        Assert.Equal(LineKind.Text, lines[8].Kind);
        Assert.Equal(LineKind.Text, lines[9].Kind); // --- is a rule, not a bullet
        Assert.Equal(LineKind.Text, lines[10].Kind); // #tag is not a heading
        Assert.True(lines[11] is { Kind: LineKind.Item, Text: "" });
        Assert.True(lines[12] is { IsTask: true, Done: true, Text: "" });
    }

    [Fact]
    public void The_view_reads_kind_state_and_level()
    {
        var document = Parse(Shopping);
        Assert.Equal([0, 2, 3, 4, 5, 6, 7, 8], document.Shown(hideDone: false));
        Assert.Equal([0, 2, 4, 5, 6, 7, 8], document.Shown(hideDone: true));
        Assert.Equal("Heading: Shopping", document.Label(0));
        Assert.Equal("Milk, task, not done", document.Label(2));
        Assert.Equal("Eggs, task, done", document.Label(3));
        Assert.Equal("Free range, task, not done, level 2", document.Label(4));
        Assert.Equal("Butter", document.Label(5));
        Assert.Equal("1. First", document.Label(6));
        Assert.Equal("A note under the list", document.Label(8));
        Assert.Equal(3, document.TaskCount);
        Assert.Equal(2, document.OpenTaskCount);
    }

    [Fact]
    public void Space_checks_and_unchecks_a_task_and_changes_that_line_alone()
    {
        var document = Parse(Shopping);
        var (done, said) = document.Toggle(2)!.Value;
        Assert.Equal("Done: Milk", said);
        Assert.Equal(Shopping.Replace("- [ ] Milk", "- [x] Milk"), done.ToText());
        Assert.Equal("Not done: Milk", done.Toggle(2)!.Value.Said);
        Assert.Null(document.Toggle(5));
    }

    [Fact]
    public void A_changed_text_keeps_the_marker_and_the_state()
    {
        var text = Parse("* [x] Old\r\n").SetText(0, "  New  ").ToText();
        Assert.Equal("* [x] New\r\n", text);
    }

    [Fact]
    public void Ctrl_T_makes_an_item_a_task_and_back()
    {
        var document = Parse("- Butter\r\n");
        var (task, said) = document.ToggleTask(0)!.Value;
        Assert.Equal("- [ ] Butter\r\n", task.ToText());
        Assert.Equal("Butter is a task", said);
        Assert.Equal("- Butter\r\n", task.ToggleTask(0)!.Value.Document.ToText());
        Assert.Null(Parse("# Heading\r\n").ToggleTask(0));
    }

    [Fact]
    public void A_new_item_follows_the_selected_one_and_its_nested_items_in_its_kind()
    {
        var (document, index) = Parse(Shopping).Insert(3, "Cheese");
        Assert.Equal(5, index);
        Assert.Equal("- [ ] Cheese", document.Lines[5].Raw);
        Assert.Equal("  - [ ] Free range", document.Lines[4].Raw);

        var (numbered, at) = Parse("1. One\r\n2. Two\r\n").Insert(0, "Between");
        Assert.Equal(1, at);
        Assert.Equal("1. One\r\n2. Between\r\n3. Two\r\n", numbered.ToText());

        var (empty, first) = Parse(string.Empty).Insert(null, "Milk");
        Assert.Equal(0, first);
        Assert.Equal("- [ ] Milk\r\n", empty.ToText());

        var (afterHeading, under) = Parse("# Shopping\r\n").Insert(0, "Milk");
        Assert.Equal(1, under);
        Assert.Equal("# Shopping\r\n- [ ] Milk\r\n", afterHeading.ToText());
    }

    [Fact]
    public void Delete_takes_the_nested_items_with_it()
    {
        var (document, nested) = Parse(Shopping).Remove(3);
        Assert.Equal(1, nested);
        Assert.DoesNotContain(document.Lines, line => line.Text is "Eggs" or "Free range");
        var (numbered, _) = Parse("1. One\r\n2. Two\r\n3. Three\r\n").Remove(0);
        Assert.Equal("1. Two\r\n2. Three\r\n", numbered.ToText());
    }

    [Fact]
    public void Alt_Up_and_Down_move_an_item_with_its_nested_items_past_its_sibling()
    {
        var document = Parse(Shopping);
        var (down, at) = document.Move(2, 1)!.Value;
        Assert.Equal(4, at);
        Assert.Equal(["Eggs", "Free range", "Milk"], down.Lines.Skip(2).Take(3).Select(line => line.Text));
        var (up, back) = down.Move(4, -1)!.Value;
        Assert.Equal(2, back);
        Assert.Equal(document.ToText(), up.ToText());
        Assert.Null(document.Move(2, -1)); // the heading is above Milk
        Assert.Null(document.Move(4, 1)); // Free range is the last at its level
        Assert.Null(document.Move(0, 1)); // a heading does not move

        var (renumbered, _) = Parse("1. One\r\n2. Two\r\n").Move(0, 1)!.Value;
        Assert.Equal("1. Two\r\n2. One\r\n", renumbered.ToText());
    }

    [Fact]
    public void Alt_Right_nests_under_the_item_before_and_Alt_Left_takes_it_out()
    {
        var document = Parse("- [ ] Milk\r\n- [ ] Bread\r\n  - [ ] Wholemeal\r\n");
        var indented = document.Indent(1, 1)!;
        Assert.Equal("- [ ] Milk\r\n  - [ ] Bread\r\n    - [ ] Wholemeal\r\n", indented.ToText());
        Assert.Equal("Bread, task, not done, level 2", indented.Label(1));
        Assert.Equal(document.ToText(), indented.Indent(1, -1)!.ToText());
        Assert.Null(document.Indent(0, 1)); // nothing before Milk at its level
        Assert.Null(document.Indent(0, -1)); // already at the top
        Assert.Equal("- [ ] Milk\r\n- [ ] Bread\r\n- [ ] Wholemeal\r\n", document.Indent(2, -1)!.ToText());
    }

    [Fact]
    public void Numbering_restarts_at_a_heading_and_continues_past_nested_lines()
    {
        var (document, _) = Parse("1. A\r\n   - nested\r\n5. B\r\n# Next\r\n7. C\r\n").Insert(4, "D");
        Assert.Equal("1. A\r\n   - nested\r\n2. B\r\n# Next\r\n1. C\r\n2. D\r\n", document.ToText());
    }

    [Fact]
    public void A_line_is_found_again_only_while_it_is_what_was_shown()
    {
        var document = Parse(Shopping);
        var shown = document.Lines[2];
        Assert.True(document.Holds(2, shown));
        var changed = Parse(Shopping.Replace("Milk", "Oat milk"));
        Assert.False(changed.Holds(2, shown));
        Assert.False(document.Holds(99, shown));
    }
}
