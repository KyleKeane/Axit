using AxStart.Core;

namespace AxStart.Tests;

/// <summary>The matching and its order (SPEC.md AS-2.2) over a fixed list of names.</summary>
public sealed class ProgramMatchTests
{
    private static readonly IReadOnlyList<ProgramEntry> Programs =
    [
        Entry("OneNote"),
        Entry("Notepad++"),
        Entry("Notepad"),
        Entry("Windows PowerShell"),
        Entry("Calculator"),
        Entry("Café Menu"),
        Entry("Sticky Notes"),
    ];

    private static readonly Dictionary<string, int> NoStarts = [];

    private static ProgramEntry Entry(string name) => new(name, name, ProgramKind.Other, null);

    private static List<string> Names(string text, IReadOnlyDictionary<string, int>? starts = null) =>
        ProgramMatch.Find(Programs, text, starts ?? NoStarts).Select(entry => entry.Name).ToList();

    [Fact]
    public void A_name_starting_with_the_text_comes_before_a_word_and_a_word_before_the_rest()
    {
        Assert.Equal(["Notepad", "Notepad++", "Sticky Notes", "OneNote"], Names("note"));
    }

    [Fact]
    public void Case_is_ignored()
    {
        Assert.Equal(["Calculator"], Names("CALC"));
    }

    [Fact]
    public void Every_word_must_be_in_the_name_in_any_order()
    {
        Assert.Equal(["Windows PowerShell"], Names("win power"));
        Assert.Equal(["Windows PowerShell"], Names("power win"));
        Assert.Empty(Names("win calc"));
    }

    [Fact]
    public void Accents_are_ignored_both_ways()
    {
        Assert.Equal(["Café Menu"], Names("cafe"));
        Assert.Equal(["Café Menu"], Names("CAFÉ"));
    }

    [Fact]
    public void The_programs_started_more_often_come_first_within_a_rank()
    {
        var starts = new Dictionary<string, int> { ["Notepad++"] = 3, ["OneNote"] = 9 };
        // OneNote stays last: a name containing the text never passes one starting with it.
        Assert.Equal(["Notepad++", "Notepad", "Sticky Notes", "OneNote"], Names("note", starts));
    }

    [Fact]
    public void An_empty_text_matches_every_program_the_most_started_first()
    {
        var starts = new Dictionary<string, int> { ["Calculator"] = 2 };
        var names = Names("  ", starts);
        Assert.Equal(Programs.Count, names.Count);
        Assert.Equal("Calculator", names[0]);
    }

    [Fact]
    public void A_text_nothing_matches_says_so()
    {
        Assert.Empty(Names("xyz"));
        Assert.Equal("No programs match xyz", ProgramMatch.NoMatch(" xyz "));
    }

    [Fact]
    public void A_match_is_said_with_its_place()
    {
        Assert.Equal("Calculator, 1 of 2", ProgramMatch.Spoken(Entry("Calculator"), 0, 2));
    }

    [Fact]
    public void The_same_name_twice_is_one_entry()
    {
        var both = new[] { Entry("Notepad"), Entry("notepad"), Entry(" "), Entry("Calculator") };
        Assert.Equal(["Notepad", "Calculator"], ProgramMatch.Distinct(both).Select(entry => entry.Name));
    }

    [Fact]
    public void A_parsing_name_tells_the_kind_and_the_path()
    {
        var programFiles = Guid.Parse("6D809377-6AF0-444B-8957-A3773F02200E");
        string? Folder(Guid id) => id == programFiles ? @"C:\Program Files" : null;

        var desktop = ProgramEntry.From("Notepad++", "{6D809377-6AF0-444B-8957-A3773F02200E}\\Notepad++\\notepad++.exe", Folder);
        Assert.Equal(ProgramKind.Desktop, desktop.Kind);
        Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", desktop.Target);
        Assert.Equal(@"Notepad++, desktop program, C:\Program Files\Notepad++\notepad++.exe", desktop.Information);

        var store = ProgramEntry.From("Calculator", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", Folder);
        Assert.Equal(ProgramKind.Store, store.Kind);
        Assert.Equal("Calculator, Store app", store.Information);

        Assert.Equal(@"C:\Tools\tool.exe", ProgramEntry.From("Tool", @"C:\Tools\tool.exe", Folder).Target);
        Assert.Equal(ProgramKind.Other, ProgramEntry.From("Chrome", "Chrome", Folder).Kind);
        Assert.Equal(ProgramKind.Other, ProgramEntry.From("Odd", "{00000000-0000-0000-0000-000000000001}\\x.exe", Folder).Kind);
    }
}
