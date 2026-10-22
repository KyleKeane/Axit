using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The Actions list (docs/axfile/SPEC.md AF-9.2, AF-9.3): the lines in their sections, their states, where Enter goes, and the limits.</summary>
public sealed class ActionLogTests
{
    private static readonly string Folder = @"C:\work";

    private static string In(string name) => Path.Combine(Folder, name);

    private static ActionLog Log(DateTime at) => new(() => at);

    [Fact]
    public void A_running_line_is_replaced_when_it_ends_and_an_instant_one_is_done_at_once()
    {
        var log = Log(new DateTime(2026, 10, 22, 21, 4, 0));
        var copy = log.Begin("Copying 3 items", Folder);
        Assert.Equal(["Running", "Copying 3 items"], log.Lines());
        Assert.False(copy.CanUndo);
        Assert.Equal((Folder, (string?)null), copy.Location);
        Assert.Null(log.EntryAt(0));
        Assert.Same(copy, log.EntryAt(1));
        Assert.Equal("Running", log.Information(0));

        log.Progress(copy, "Copying 3 items, please wait");
        Assert.Equal("Copying 3 items, please wait", copy.Line);

        var operation = new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt", @"C:\elsewhere\b.txt", @"C:\elsewhere\c.txt"], [In("a.txt"), In("b.txt"), In("c.txt")]);
        log.End(copy, "Copied 3 items", operation);
        Assert.Equal(["Operations", "Copied 3 items"], log.Lines());
        Assert.True(copy.CanUndo);
        Assert.Equal((Folder, "a.txt"), copy.Location);
        Assert.Same(copy, log.Newest());
        Assert.Equal("Un&do paste", log.MenuText);
        Assert.Equal("Operations, 1 of 1, 21:04", log.Information(1));

        var rename = log.Add("Renamed notes.md to todo.md", new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]));
        Assert.Equal(["Operations", "Copied 3 items", "Renamed notes.md to todo.md"], log.Lines());
        Assert.Equal((Folder, "todo.md"), rename.Location);
        Assert.Same(rename, log.Newest());
        Assert.Equal("Un&do rename", log.MenuText);
        Assert.Equal(2, log.IndexOf(rename));
        Assert.Same(rename, log.Latest);
        Assert.Same(rename, log.LatestOperation);
    }

    [Fact]
    public void Running_lines_come_first_whatever_their_time()
    {
        var log = Log(DateTime.Now);
        var rename = log.Add("Renamed notes.md to todo.md", new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]));
        var copy = log.Begin("Copying 3 items", Folder);
        Assert.Equal(["Running", "Copying 3 items", "Operations", "Renamed notes.md to todo.md"], log.Lines());
        Assert.Equal(1, log.IndexOf(copy));
        Assert.Equal(3, log.IndexOf(rename));
        Assert.Same(copy, log.Latest);

        log.End(copy, "Copied 3 items", null);
        Assert.Equal(["Operations", "Renamed notes.md to todo.md", "Copied 3 items"], log.Lines());
        Assert.Equal(2, log.IndexOf(copy));
        Assert.False(copy.CanUndo);
    }

    [Fact]
    public void A_failed_line_says_so_and_cannot_be_undone()
    {
        var log = Log(DateTime.Now);
        var zip = log.Begin("Compressing", Folder);
        log.Progress(zip, "Compressing, 40 of 120 files");
        log.Fail(zip, "The file is in use");
        Assert.Equal(["Operations", "Failed: The file is in use"], log.Lines());
        Assert.Equal(ActionState.Failed, zip.State);
        Assert.False(zip.CanUndo);
        Assert.Null(log.Newest());
        Assert.Equal("Un&do", log.MenuText);
        // A progress report after the end changes nothing.
        log.Progress(zip, "Compressing, 50 of 120 files");
        Assert.Equal("Failed: The file is in use", zip.Line);
    }

    [Fact]
    public void An_undone_line_reads_so_and_enter_goes_where_the_undo_put_things_back()
    {
        var log = Log(DateTime.Now);
        var rename = log.Add("Renamed notes.md to todo.md", new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]));
        var made = log.Add("Created temp", new Operation(OperationKind.NewFolder, [], [In("temp")]));
        log.Undone(made);
        Assert.Equal("Created temp, undone", made.Line);
        Assert.False(made.CanUndo);
        // What was made is gone: Enter goes to the folder it was in.
        Assert.Equal((Folder, (string?)null), made.Location);
        Assert.Same(rename, log.Newest());

        log.Undone(rename);
        Assert.Equal((Folder, "notes.md"), rename.Location);
        Assert.Null(log.Newest());

        var moved = log.Add("Moved 2 items into bundle", new Operation(OperationKind.Move, [In("a.txt"), In("b.txt")], [Path.Combine(Folder, "bundle", "a.txt"), Path.Combine(Folder, "bundle", "b.txt")], In("bundle")));
        Assert.Equal((Folder, "bundle"), moved.Location);
        log.NotUndone(moved, "a.txt has changed");
        Assert.Equal("Moved 2 items into bundle, not undone: a.txt has changed", moved.Line);
        Assert.False(moved.CanUndo);
        Assert.Null(log.Newest());
    }

    [Fact]
    public void A_delete_leads_back_to_its_folder_and_to_the_restored_entry_once_undone()
    {
        var log = Log(DateTime.Now);
        var delete = log.Begin("Deleting 2 items", Folder);
        log.End(delete, "Deleted 2 items", new Operation(OperationKind.Delete, [In("a.txt"), In("b.txt")], []));
        Assert.Equal((Folder, (string?)null), delete.Location);
        log.Undone(delete);
        Assert.Equal((Folder, "a.txt"), delete.Location);
    }

    [Fact]
    public void Fifty_lines_stay_and_twenty_can_be_undone()
    {
        var log = Log(DateTime.Now);
        for (var i = 1; i <= 55; i++)
        {
            log.Add($"Created {i}", new Operation(OperationKind.NewFolder, [], [In(i.ToString())]));
        }

        Assert.Equal(ActionLog.Limit, log.Count);
        Assert.Equal("Operations", log.Lines()[0]);
        Assert.Equal("Created 6", log.Lines()[1]);
        Assert.Equal("Created 55", log.Lines()[^1]);
        Assert.Equal(ActionLog.UndoLimit, log.Entries.Count(entry => entry.CanUndo));
        Assert.False(log.Entries[0].CanUndo);
        Assert.True(log.Entries[^1].CanUndo);
        // The oldest that lost its operation keeps its line and its place.
        Assert.Equal((Folder, "36"), log.Entries[^20].Location);
        Assert.Equal((Folder, "35"), log.Entries[^21].Location);
        Assert.Null(log.Entries[^21].Operation);
    }

    [Fact]
    public void Selection_lines_sit_last_are_undone_on_their_line_and_go_with_the_folder()
    {
        var log = Log(new DateTime(2026, 10, 22, 21, 4, 0));
        var rename = log.Add("Renamed notes.md to todo.md", new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]));
        var mark = log.Mark("Selected a.txt", Folder, "a.txt", []);
        var range = log.Mark("Selected 2 items", Folder, "c.txt", [In("a.txt")]);
        Assert.Equal(["Operations", "Renamed notes.md to todo.md", "Selection", "Selected a.txt", "Selected 2 items"], log.Lines());
        Assert.Equal(ActionSection.Selection, mark.Section);
        Assert.Equal((Folder, "a.txt"), mark.Location);
        Assert.Same(range, log.Latest);
        Assert.Same(rename, log.LatestOperation);
        // Ctrl+Z in the folder list never takes a selection line.
        Assert.Same(rename, log.Newest());
        Assert.Equal("Selection, 2 of 2, 21:04", log.Information(4));
        Assert.Equal("Selection", log.Information(2));
        Assert.Null(log.EntryAt(2));

        // The range grows: its line is rewritten in place.
        log.Rewrite(range, "Selected 3 items");
        Assert.Equal("Selected 3 items", range.Line);
        Assert.True(range.CanUndo);
        Assert.Equal([In("a.txt")], range.Before);

        log.Undone(range);
        Assert.Equal("Selected 3 items, undone", range.Line);
        Assert.False(range.CanUndo);
        log.Rewrite(range, "Selected 4 items");
        Assert.Equal("Selected 3 items, undone", range.Line);

        // Twenty selection lines stay, the operations untouched.
        for (var i = 1; i <= 25; i++)
        {
            log.Mark($"Selected {i}.txt", Folder, $"{i}.txt", []);
        }

        Assert.Equal(ActionLog.SelectionLimit, log.Entries.Count(entry => entry.Kind == ActionKind.Selection));
        Assert.Equal("Selected 6.txt", log.Lines()[3]);
        Assert.Equal(-1, log.IndexOf(mark));
        Assert.Equal(1, log.IndexOf(rename));

        log.DropSelection();
        Assert.Equal(["Operations", "Renamed notes.md to todo.md"], log.Lines());
        Assert.Same(rename, log.Latest);
    }

    [Fact]
    public void An_operation_knows_where_it_left_things()
    {
        Assert.Equal(Folder, new Operation(OperationKind.Rename, [In("a")], [In("b")]).ResultFolder);
        Assert.Equal("b", new Operation(OperationKind.Rename, [In("a")], [In("b")]).Result);
        var move = new Operation(OperationKind.Move, [In("a")], [Path.Combine(Folder, "bundle", "a")], In("bundle"));
        Assert.Equal(Folder, move.ResultFolder);
        Assert.Equal("bundle", move.Result);
        var paste = new Operation(OperationKind.Move, [@"C:\elsewhere\a"], [In("a")]);
        Assert.Equal(Folder, paste.ResultFolder);
        Assert.Equal("a", paste.Result);
        var delete = new Operation(OperationKind.Delete, [In("a"), In("b")], []);
        Assert.Equal(Folder, delete.ResultFolder);
        Assert.Null(delete.Result);
        Assert.Equal("1 item", ActionLog.Items(1));
        Assert.Equal("5 items", ActionLog.Items(5));
    }
}
