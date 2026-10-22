using System.Text;
using AxFile.Core;

namespace AxFile.Tests;

/// <summary>Undo (docs/axfile/SPEC.md AF-4.14): the history, each operation's inverse over a temporary folder, and the Recycle Bin's records; the Recycle Bin itself is not exercised.</summary>
public sealed class UndoTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axfile-undo-" + Guid.NewGuid().ToString("N"));

    public UndoTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "docs"));
        File.WriteAllText(Path.Combine(_folder, "notes.md"), "# Notes");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string In(string name) => Path.Combine(_folder, name);

    [Fact]
    public void The_history_keeps_the_last_twenty_and_names_the_menu_item()
    {
        var history = new OperationHistory();
        Assert.Equal("Un&do", history.MenuText);
        Assert.Null(history.Pop());
        for (var i = 0; i < 25; i++)
        {
            history.Push(new Operation(OperationKind.NewFile, [], [In($"{i}.txt")]));
        }

        history.Push(new Operation(OperationKind.Rename, [In("a")], [In("b")]));
        Assert.Equal(OperationHistory.Limit, history.Count);
        Assert.Equal("Un&do rename", history.MenuText);
        Assert.Equal(OperationKind.Rename, history.Pop()!.Kind);
        Assert.Equal("Un&do new text file", history.MenuText);
        history.Clear();
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void An_operation_knows_its_folder_its_entry_and_its_texts()
    {
        var rename = new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]);
        Assert.Equal(_folder, rename.Folder);
        Assert.Equal("notes.md", rename.Select);
        Assert.Equal("Undone: renamed back to notes.md", rename.DoneText);

        var copy = new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt", @"C:\elsewhere\b.txt"], [In("a.txt"), In("b.txt")]);
        Assert.Equal(_folder, copy.Folder);
        Assert.Null(copy.Select);
        Assert.Equal("Undone: 2 items sent to the Recycle Bin", copy.DoneText);

        var delete = new Operation(OperationKind.Delete, [In("notes.md")], []);
        Assert.Equal("notes.md", delete.Select);
        Assert.Equal("Undone: notes.md restored", delete.DoneText);
        Assert.Equal("Undone: docs removed", new Operation(OperationKind.NewFolder, [], [In("docs")]).DoneText);
        Assert.Equal("Undone: moved back", new Operation(OperationKind.Move, [In("notes.md")], [Path.Combine(_folder, "docs", "notes.md")], In("docs")).DoneText);
    }

    [Fact]
    public async Task A_rename_is_renamed_back_unless_the_entry_changed_or_the_old_name_is_taken()
    {
        var renamed = FileOperations.Rename(In("notes.md"), "todo.md");
        var operation = new Operation(OperationKind.Rename, [In("notes.md")], [renamed]);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.True(File.Exists(In("notes.md")));
        Assert.False(File.Exists(renamed));

        Assert.Equal("Not undone: todo.md has changed", FileOperations.CheckUndo(operation));
        File.WriteAllText(renamed, "x");
        Assert.Equal("Not undone: notes.md is already here", FileOperations.CheckUndo(operation));
    }

    [Fact]
    public async Task A_new_folder_or_file_is_removed_while_still_empty()
    {
        var made = FileOperations.CreateFolder(_folder, "new");
        var folder = new Operation(OperationKind.NewFolder, [], [made]);
        File.WriteAllText(Path.Combine(made, "x.txt"), "x");
        Assert.Equal("Not undone: new has files now", FileOperations.CheckUndo(folder));
        File.Delete(Path.Combine(made, "x.txt"));
        Assert.Null(FileOperations.CheckUndo(folder));
        Assert.Null(await FileOperations.UndoAsync(folder));
        Assert.False(Directory.Exists(made));

        var file = new Operation(OperationKind.NewFile, [], [FileOperations.CreateFile(_folder)]);
        Assert.Null(FileOperations.CheckUndo(file));
        File.WriteAllText(file.After[0], "typed");
        Assert.Equal("Not undone: New file.txt has content now", FileOperations.CheckUndo(file));
        File.WriteAllText(file.After[0], string.Empty);
        Assert.Null(await FileOperations.UndoAsync(file));
        Assert.False(File.Exists(file.After[0]));
    }

    [Fact]
    public async Task A_move_into_a_new_folder_goes_back_and_the_folder_goes_when_empty()
    {
        var made = FileOperations.CreateFolder(_folder, "bundle");
        var sources = new[] { In("notes.md") };
        var destinations = FileOperations.Destinations(sources, made);
        Assert.Null(await FileOperations.CopyOrMoveAsync(sources, destinations, move: true));
        var operation = new Operation(OperationKind.Move, sources, destinations, made);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.True(File.Exists(In("notes.md")));
        Assert.False(Directory.Exists(made));
    }

    [Fact]
    public async Task A_change_of_case_only_is_renamed_back()
    {
        var renamed = FileOperations.Rename(In("notes.md"), "Notes.md");
        var operation = new Operation(OperationKind.Rename, [In("notes.md")], [renamed]);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.Equal(["docs", "notes.md"], Directory.EnumerateFileSystemEntries(_folder).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void A_folder_where_the_new_file_was_is_a_change()
    {
        var file = new Operation(OperationKind.NewFile, [], [FileOperations.CreateFile(_folder)]);
        File.Delete(file.After[0]);
        Directory.CreateDirectory(file.After[0]);
        Assert.Equal("Not undone: New file.txt has changed", FileOperations.CheckUndo(file));
        var folder = new Operation(OperationKind.NewFolder, [], [In("made")]);
        File.WriteAllText(In("made"), "x");
        Assert.Equal("Not undone: made has changed", FileOperations.CheckUndo(folder));
    }

    [Fact]
    public void A_copy_whose_copies_are_gone_and_a_delete_whose_entry_is_back_are_not_undone()
    {
        Assert.Equal("Not undone: gone.txt has changed", FileOperations.CheckUndo(new Operation(OperationKind.Copy, [In("notes.md")], [In("gone.txt")])));
        Assert.Equal("Not undone: notes.md is already here", FileOperations.CheckUndo(new Operation(OperationKind.Delete, [In("notes.md")], [])));
        Assert.Null(FileOperations.CheckUndo(new Operation(OperationKind.Delete, [In("gone.txt")], [])));
    }

    [Fact]
    public void A_recycle_bin_record_gives_the_original_path_and_the_time()
    {
        var path = @"D:\Documents\notes.md";
        var chars = Encoding.Unicode.GetBytes(path + "\0");
        var bytes = new byte[24 + 4 + chars.Length];
        BitConverter.GetBytes(2L).CopyTo(bytes, 0);
        BitConverter.GetBytes(7L).CopyTo(bytes, 8);
        BitConverter.GetBytes(133_000_000_000_000_000L).CopyTo(bytes, 16);
        BitConverter.GetBytes(path.Length + 1).CopyTo(bytes, 24);
        chars.CopyTo(bytes, 28);
        Assert.Equal(new RecycleRecord(path, 133_000_000_000_000_000L), RecycleBin.Parse(bytes));

        // The older record without the length field: the path is a fixed field, read to its first null.
        var old = new byte[24 + 520];
        BitConverter.GetBytes(1L).CopyTo(old, 0);
        chars.CopyTo(old, 24);
        Assert.Equal(path, RecycleBin.Parse(old)!.OriginalPath);
        Assert.Null(RecycleBin.Parse(new byte[10]));
        Assert.Null(RecycleBin.ReadRecord(In("notes.md")));
        Assert.Null(RecycleBin.ReadRecord(In("$Rabc.md")));
    }
}
