using Axit.Core;

namespace AxList.Core;

/// <summary>One list of the folder (SPEC.md LI-3.1): its name (the file's without ".md"), its file, and its counts.</summary>
public sealed record ListSummary(string Name, string Path, int Tasks, int OpenTasks, bool Empty)
{
    /// <summary>"Shopping, 4 open tasks", "Shopping, no open tasks", "Ideas", "Ideas, empty".</summary>
    public string Label => Empty ? $"{Name}, empty"
        : Tasks == 0 ? Name
        : OpenTasks == 0 ? $"{Name}, no open tasks"
        : $"{Name}, {OpenTasks} open {(OpenTasks == 1 ? "task" : "tasks")}";
}

/// <summary>An open task of the Tasks view (LI-5.1): the list it is in, its line in that file, and the line as read.</summary>
public sealed record OpenTask(string ListName, string Path, int Index, ListLine Line)
{
    public string Label => $"{(Line.Text.Length == 0 ? "(empty)" : Line.Text)}, in {ListName}";
}

/// <summary>
/// The folder of lists (SPEC.md LI-1.2, LI-3, LI-5): its <c>.md</c> files in File Explorer's order and the open tasks of
/// all of them, read through the editors' <see cref="TextDocument"/>. A file that cannot be read is listed by its name
/// and left out of the tasks.
/// </summary>
public static class ListFolder
{
    /// <summary>The folder of lists at first: Documents\Lists (LI-1.2).</summary>
    public static string DefaultFolder => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lists");

    public static IReadOnlyList<string> Files(string folder) =>
        Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.md").Where(file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => System.IO.Path.GetFileNameWithoutExtension(file), NaturalOrder.Instance).ToList()
            : [];

    public static string NameOf(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

    /// <summary>Every list of the folder with its counts.</summary>
    public static IReadOnlyList<ListSummary> Lists(string folder)
    {
        var lists = new List<ListSummary>();
        foreach (var file in Files(folder))
        {
            var document = TryRead(file);
            lists.Add(document is null
                ? new ListSummary(NameOf(file), file, 0, 0, Empty: false)
                : new ListSummary(NameOf(file), file, document.TaskCount, document.OpenTaskCount, document.Lines.All(line => line.Kind == LineKind.Blank)));
        }

        return lists;
    }

    /// <summary>The open tasks of every list, in the order of the lists and of their files.</summary>
    public static IReadOnlyList<OpenTask> OpenTasks(string folder)
    {
        var tasks = new List<OpenTask>();
        foreach (var file in Files(folder))
        {
            if (TryRead(file) is not { } document)
            {
                continue;
            }

            for (var i = 0; i < document.Lines.Count; i++)
            {
                if (document.Lines[i] is { IsTask: true, Done: false } line)
                {
                    tasks.Add(new OpenTask(NameOf(file), file, i, line));
                }
            }
        }

        return tasks;
    }

    /// <summary>The reason a name cannot be a new list's (LI-3.2), or null when it can.</summary>
    public static string? RefusedName(string folder, string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Type the list's name";
        }

        if (trimmed.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 || trimmed.EndsWith('.'))
        {
            return "A list's name cannot hold \\ / : * ? \" < > | or end in a full stop";
        }

        return File.Exists(System.IO.Path.Combine(folder, trimmed + ".md")) ? $"There is a list called {trimmed} already" : null;
    }

    /// <summary>A file's document, or null when it cannot be read.</summary>
    public static ListDocument? TryRead(string path)
    {
        try
        {
            return ListDocument.Parse(TextDocument.Read(path).Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }
}
