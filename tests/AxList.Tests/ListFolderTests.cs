using AxList.Core;

namespace AxList.Tests;

/// <summary>The folder of lists and its open tasks (docs/axlist/SPEC.md LI-3, LI-5), and the settings (LI-8.1), in a temporary folder.</summary>
public sealed class ListFolderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axlist-tests-" + Guid.NewGuid().ToString("N"));

    public ListFolderTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void The_lists_are_the_md_files_in_File_Explorers_order_with_their_counts()
    {
        File.WriteAllText(Path.Combine(_folder, "List 10.md"), "- [ ] a\r\n");
        File.WriteAllText(Path.Combine(_folder, "List 2.md"), "- [x] done\r\n");
        File.WriteAllText(Path.Combine(_folder, "Ideas.md"), "- an idea\r\n");
        File.WriteAllText(Path.Combine(_folder, "Blank.md"), "\r\n");
        File.WriteAllText(Path.Combine(_folder, "notes.txt"), "- [ ] not a list\r\n");
        var lists = ListFolder.Lists(_folder);
        Assert.Equal(["Blank, empty", "Ideas", "List 2, no open tasks", "List 10, 1 open task"], lists.Select(list => list.Label));
    }

    [Fact]
    public void The_open_tasks_of_every_list_come_in_order()
    {
        File.WriteAllText(Path.Combine(_folder, "Home.md"), "# Home\r\n- [ ] Fix the tap\r\n- [x] Paint\r\n");
        File.WriteAllText(Path.Combine(_folder, "Work.md"), "1. [ ] Write the report\r\n");
        var tasks = ListFolder.OpenTasks(_folder);
        Assert.Equal(["Fix the tap, in Home", "Write the report, in Work"], tasks.Select(task => task.Label));
        Assert.Equal(1, tasks[0].Index);
    }

    [Fact]
    public void A_new_lists_name_is_refused_with_its_reason()
    {
        File.WriteAllText(Path.Combine(_folder, "Shopping.md"), string.Empty);
        Assert.Equal("Type the list's name", ListFolder.RefusedName(_folder, "  "));
        Assert.Equal("There is a list called shopping already", ListFolder.RefusedName(_folder, "shopping"));
        Assert.NotNull(ListFolder.RefusedName(_folder, "a/b"));
        Assert.NotNull(ListFolder.RefusedName(_folder, "end."));
        Assert.Null(ListFolder.RefusedName(_folder, "Course plan"));
        Assert.Empty(ListFolder.Files(Path.Combine(_folder, "missing")));
    }

    [Fact]
    public void The_settings_keep_the_folder_the_last_list_and_hide_done()
    {
        var path = Path.Combine(_folder, "axlist.json");
        var settings = ListSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(ListFolder.DefaultFolder, settings.ListsFolder);
        settings.Folder = _folder;
        settings.LastList = "Shopping.md";
        settings.HideDone = true;
        settings.Save(path);
        var loaded = ListSettings.Load(path, out _);
        Assert.Equal(_folder, loaded.ListsFolder);
        Assert.Equal("Shopping.md", loaded.LastList);
        Assert.True(loaded.HideDone);
    }
}
