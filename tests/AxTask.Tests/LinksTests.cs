using System.Text.Json.Nodes;
using AxTask.Core;
using Axit.Core;

namespace AxTask.Tests;

public sealed class LinksTests : IDisposable
{
    private readonly TempFolder _top = new("axtask-links");

    public LinksTests()
    {
        Directory.CreateDirectory(Path.Combine(_top.Path, "Home"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Shopping"));
        Write(Path.Combine(_top.Path, "Shopping", "Washer.json"), ItemDocument.NewItem("Washer", []));
        Write(Path.Combine(_top.Path, "Home", "Fix the tap.json"), WithLink("Fix the tap", "../Shopping/Washer.json"));
        Write(Path.Combine(_top.Path, "Shopping", "Spanner.json"), WithLink("Spanner", "../Home/Fix the tap.json"));
    }

    public void Dispose() => _top.Dispose();

    [Fact]
    public void AnItemMovedKeepsTheLinksToIt()
    {
        var from = Path.Combine(_top.Path, "Shopping", "Washer.json");
        var to = Path.Combine(_top.Path, "Archive", "Washer.json");
        var writes = Links.AfterMove(ItemFolder.AllItems(_top.Path), from, to);
        var write = Assert.Single(writes);
        Assert.Equal(Path.Combine(_top.Path, "Home", "Fix the tap.json"), write.Path);
        Assert.Contains("../Archive/Washer.json", write.After);
    }

    [Fact]
    public void AListMovedDeeperKeepsTheLinksItHolds()
    {
        var from = Path.Combine(_top.Path, "Home");
        var to = Path.Combine(_top.Path, "Archive", "Home");
        var writes = Links.AfterMove(ItemFolder.AllItems(_top.Path), from, to);
        Assert.Equal(2, writes.Count);
        var inside = writes.Single(write => write.Path == Path.Combine(to, "Fix the tap.json"));
        Assert.Contains("../../Shopping/Washer.json", inside.After);
        var outside = writes.Single(write => write.Path == Path.Combine(_top.Path, "Shopping", "Spanner.json"));
        Assert.Contains("../Archive/Home/Fix the tap.json", outside.After);
    }

    [Fact]
    public void ALinkReadsItsItemOrMissing()
    {
        var from = Path.Combine(_top.Path, "Home", "Fix the tap.json");
        Assert.Equal("Washer", Links.Spoken(from, "../Shopping/Washer.json"));
        Assert.Equal("missing: Hose", Links.Spoken(from, "../Shopping/Hose.json"));
    }

    [Fact]
    public void ACheckComesAndGoes()
    {
        Assert.Empty(TaskRules.TemplateOf(ItemDocument.NewList([]).WithTemplate([])));
        Assert.Equal(3, TaskRules.TemplateOf(null).Count);
        var plain = ItemDocument.NewItem("Butter", []);
        var task = TaskRules.WithDone(plain);
        Assert.Equal(["Title", "Done"], task.Fields.Select(field => field.Title));
        Assert.Same(task, TaskRules.WithDone(task));
        Assert.Equal(["Title"], TaskRules.WithoutDone(task).Fields.Select(field => field.Title));
        Assert.Equal(["Done", "Due"], TaskRules.TemplateWithDone(TaskRules.TemplateWithDone(TaskRules.DefaultTemplate, false).Where(f => f.Title != "Start").ToList(), true).Select(f => f.Title));
    }

    [Fact]
    public void ANewListsFolderGoesWithItsUndo()
    {
        var path = Path.Combine(_top.Path, "Garden", ItemDocument.ListFile);
        var step = new Step([FileChange.WriteInNewFolder(path, ItemDocument.NewList(TaskRules.DefaultTemplate).ToText())], "Made Garden");
        Changes.Apply(step.Parts);
        Assert.True(Directory.Exists(Path.Combine(_top.Path, "Garden")));
        Changes.Undo(step);
        Assert.False(Directory.Exists(Path.Combine(_top.Path, "Garden")));
    }

    private static ItemDocument WithLink(string title, string link) =>
        ItemDocument.NewItem(title, [new ItemField("Waits on", FieldTypes.Link, JsonValue.Create(link), [])]);

    private static void Write(string path, ItemDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, document.ToText());
    }

    /// <summary>S-5: a link that is not relative, or leads off this computer, is never opened; it reads as missing.</summary>
    [Fact]
    public void A_rooted_or_network_link_is_never_followed()
    {
        var from = Path.Combine(Path.GetTempPath(), "tasks", "Home", "Fix.json");
        Assert.Null(ItemFolder.Resolve(from, "//server/share/x.json"));
        Assert.Null(ItemFolder.Resolve(from, @"\\server\share\x.json"));
        Assert.Null(ItemFolder.Resolve(from, "C:/Windows/win.ini"));
        Assert.Equal("missing: x", Links.Spoken(from, "//server/share/x.json"));
    }
}