using AxTask.Core;
using Axit.Core;

namespace AxTask.Tests;

/// <summary>The folder of lists, the changes with their undo, the tasks and the smart lists, over a temporary folder.</summary>
public sealed class FolderTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 24);

    private readonly string _top = Path.Combine(Path.GetTempPath(), "axtask-tests-" + Guid.NewGuid().ToString("N"));

    public FolderTests()
    {
        Directory.CreateDirectory(Path.Combine(_top, "Home"));
        Directory.CreateDirectory(Path.Combine(_top, "Work", "Course plan"));
        Directory.CreateDirectory(Path.Combine(_top, "Archive"));
        Directory.CreateDirectory(Path.Combine(_top, ".hidden"));
        Item("Home", "Fix the tap", done: false, due: "2026-10-30");
        Item("Home", "Water plants", done: true, due: "2026-10-20");
        Item("Work", "Report", done: false, due: "2026-10-22");
        Item(Path.Combine("Work", "Course plan"), "Week one", done: false, due: null);
        Item("Archive", "Old thing", done: false, due: null);
        File.WriteAllText(Path.Combine(_top, "Home", "notes.txt"), "not an item");
        File.WriteAllText(Path.Combine(_top, "Home", "broken.json"), "{ nope");
        File.WriteAllText(Path.Combine(_top, "Home", "Milk.json"), ItemDocument.NewItem("Milk", []).ToText());
    }

    public void Dispose() => Directory.Delete(_top, recursive: true);

    [Fact]
    public void ReadsALevel()
    {
        var top = ItemFolder.Read(_top, isTop: true);
        Assert.Equal(["Home", "Work", "Archive"], top.Lists.Select(Path.GetFileName));
        var home = ItemFolder.Read(Path.Combine(_top, "Home"), isTop: false);
        Assert.Equal(["Fix the tap", "Milk", "Water plants"], home.Items.Select(ItemFolder.NameOf));
        Assert.Equal(2, home.NotShown);
    }

    [Fact]
    public void AllTasksLeavesOutTheDoneThePlainAndTheArchive()
    {
        var tasks = SmartList.AllTasks.Items(_top, Today);
        Assert.Equal(["Report", "Fix the tap", "Week one"], tasks.Select(ItemFolder.NameOf));
    }

    [Fact]
    public void ASmartListFilters()
    {
        File.WriteAllText(Path.Combine(_top, "Work soon.filter.json"), """
            { "format": "axit-filter 1", "match": "all",
              "conditions": [ { "field": "Done", "is": false }, { "list": "Work" }, { "field": "Due", "before": "+7" } ] }
            """);
        File.WriteAllText(Path.Combine(_top, "Odd.filter.json"), """{ "conditions": [ { "field": "Due", "soon": true } ] }""");
        var lists = SmartList.InFolder(_top);
        Assert.Equal(["Odd", "Work soon"], lists.Select(list => list.Name));
        Assert.NotNull(lists[0].NotUnderstood);
        Assert.Empty(lists[0].Items(_top, Today));
        Assert.Equal(["Report"], lists[1].Items(_top, Today).Select(ItemFolder.NameOf));
        Assert.DoesNotContain(ItemFolder.Read(_top, isTop: true).Items, item => item.Path.EndsWith(".filter.json"));
    }

    [Fact]
    public void ANewSmartListIsAllTasks()
    {
        var path = Path.Combine(_top, "Mine.filter.json");
        File.WriteAllText(path, SmartList.NewFileText());
        Assert.Equal(SmartList.AllTasks.Items(_top, Today).Select(item => item.Path), SmartList.Read(path).Items(_top, Today).Select(item => item.Path));
    }

    [Fact]
    public void SummariesSayDoneDueAndOverdue()
    {
        var home = ItemFolder.Read(Path.Combine(_top, "Home"), isTop: false).Items;
        Assert.Equal(", due 30 October", TaskRules.Summary(home[0].Document, Today));
        Assert.Equal(string.Empty, TaskRules.Summary(home[1].Document, Today));
        Assert.Equal(", done, due 20 October", TaskRules.Summary(home[2].Document, Today));
        var report = ItemFolder.Read(Path.Combine(_top, "Work"), isTop: false).Items[0];
        Assert.Equal(", overdue since 22 October", TaskRules.Summary(report.Document, Today));
    }

    [Theory]
    [InlineData("Fix the tap", "Fix the tap")]
    [InlineData("a/b: c?", "a b c")]
    [InlineData("  ", "Untitled")]
    [InlineData("Ends with.", "Ends with")]
    public void FileNamesComeFromTheTitle(string title, string stem) => Assert.Equal(stem, ItemFolder.FileStem(title));

    [Fact]
    public void ATakenNameGetsANumber()
    {
        var home = Path.Combine(_top, "Home");
        Assert.Equal(Path.Combine(home, "Fix the tap - 2.json"), ItemFolder.FreePath(home, "Fix the tap", ".json"));
        var own = Path.Combine(home, "Fix the tap.json");
        Assert.Equal(own, ItemFolder.FreePath(home, "Fix the tap", ".json", own));
        Assert.Equal("There is a list called home here already", ItemFolder.RefusedListName(_top, "home"));
        Assert.NotNull(ItemFolder.RefusedListName(_top, "a/b"));
        Assert.Null(ItemFolder.RefusedListName(_top, "Garden"));
    }

    [Fact]
    public void LinksArePathsFromTheItem()
    {
        var from = Path.Combine(_top, "Home", "Fix the tap.json");
        var to = Path.Combine(_top, "Work", "Report.json");
        Assert.Equal("../Work/Report.json", ItemFolder.LinkTo(from, to));
        Assert.Equal(to, ItemFolder.Resolve(from, "../Work/Report.json"));
    }

    [Fact]
    public void AStepIsUndoneOnlyWhenNothingChangedIt()
    {
        var path = Path.Combine(_top, "Home", "Fix the tap.json");
        var before = File.ReadAllText(path).ReplaceLineEndings("\r\n");
        var after = ItemDocument.Parse(before).WithTitle("Fix the sink").ToText();
        var moved = Path.Combine(_top, "Home", "Fix the sink.json");
        var step = new Step([FileChange.Write(path, before, after), FileChange.Move(path, moved)], "Renamed");
        Changes.Apply(step.Parts);
        Assert.False(File.Exists(path));
        Assert.Equal("Fix the sink", ItemFolder.ReadItem(moved)!.Document.Title);
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.False(File.Exists(moved));
        Assert.Equal("Fix the tap", ItemFolder.ReadItem(path)!.Document.Title);

        Changes.Apply(step.Parts);
        File.WriteAllText(moved, ItemDocument.NewItem("Changed elsewhere", []).ToText());
        Assert.Equal("Fix the sink changed on disk", Changes.CheckUndo(step));
    }

    [Fact]
    public void AMadeFileIsRemovedByUndo()
    {
        var path = Path.Combine(_top, "Garden", "Dig.json");
        var step = new Step([FileChange.Write(path, null, ItemDocument.NewItem("Dig", []).ToText())], "Added Dig");
        Changes.Apply(step.Parts);
        Assert.True(File.Exists(path));
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void AFolderMovesAndComesBack()
    {
        var from = Path.Combine(_top, "Work");
        var to = Path.Combine(_top, "Archive", "Work");
        var step = new Step([FileChange.Move(from, to)], "Archived Work");
        Changes.Apply(step.Parts);
        Assert.True(Directory.Exists(to));
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.True(Directory.Exists(from));
    }

    [Fact]
    public void TheArchiveIsKnown()
    {
        Assert.True(ItemFolder.IsArchive(_top, Path.Combine(_top, "Archive")));
        Assert.True(ItemFolder.IsArchive(_top, Path.Combine(_top, "archive", "x.json")));
        Assert.False(ItemFolder.IsArchive(_top, Path.Combine(_top, "Archived")));
        Assert.Equal("Work, Course plan", ItemFolder.ListName(_top, Path.Combine(_top, "Work", "Course plan")));
    }

    private void Item(string list, string title, bool done, string? due)
    {
        var item = ItemDocument.NewItem(title, TaskRules.DefaultTemplate);
        var document = item.WithValue(item.IndexOf("Done"), System.Text.Json.Nodes.JsonValue.Create(done));
        if (due is not null)
        {
            document = document.WithValue(document.IndexOf("Due"), System.Text.Json.Nodes.JsonValue.Create(due));
        }

        File.WriteAllText(Path.Combine(_top, list, title + ".json"), document.ToText());
    }
}
