namespace AxMedia.Core;

/// <summary>
/// The queue (SPEC.md AM-3): the files to play in order and the one playing. The window plays from
/// <see cref="Playing"/> and fills its list from <see cref="Lines"/>. The queue holds paths, not entries: it is
/// what was handed over and nothing changes it while the user browses elsewhere (AM-D2). From AxFile.Core
/// (2026-10-22), without the operations' status lines, which stayed AxFile's.
/// </summary>
public sealed class PlayQueue
{
    public const string EmptyText = "Queue is empty";
    public const string PlayingMark = " (playing)";

    private readonly List<string> _paths = [];

    public IReadOnlyList<string> Paths => _paths;

    public int Count => _paths.Count;

    /// <summary>The index of the file playing, or the last one played; -1 with none.</summary>
    public int Playing { get; private set; } = -1;

    public string? PlayingPath => Playing >= 0 && Playing < _paths.Count ? _paths[Playing] : null;

    public string? PlayingName => PlayingPath is { } path ? Path.GetFileName(path) : null;

    /// <summary>The queue replaced by <paramref name="paths"/> (a play from AxFile, a start with paths), playing from <paramref name="start"/>.</summary>
    public void Set(IReadOnlyList<string> paths, int start)
    {
        _paths.Clear();
        _paths.AddRange(paths);
        Playing = start >= 0 && start < _paths.Count ? start : -1;
    }

    /// <summary>Add: the paths appended; how many were.</summary>
    public int Add(IEnumerable<string> paths)
    {
        var before = _paths.Count;
        _paths.AddRange(paths);
        return _paths.Count - before;
    }

    /// <summary>Clear queue: the files gone.</summary>
    public void Clear()
    {
        _paths.Clear();
        Playing = -1;
    }

    /// <summary>Whether <paramref name="index"/> names a queued file rather than the empty text.</summary>
    public bool IsFile(int index) => index >= 0 && index < _paths.Count;

    /// <summary>The playing index moved to a queued file; false when the index is not one.</summary>
    public bool Select(int index)
    {
        if (!IsFile(index))
        {
            return false;
        }

        Playing = index;
        return true;
    }

    /// <summary>The next file to play when one ends; false at the end, the index staying on the last one.</summary>
    public bool MoveOn()
    {
        if (Playing + 1 >= _paths.Count)
        {
            return false;
        }

        Playing++;
        return true;
    }

    /// <summary>
    /// The file at <paramref name="index"/> taken out (Delete in the list): its name, or null when the index is no
    /// file. The playing index follows its file; when the file playing is the one removed, the index stays and now
    /// names the file after it, or -1 past the end, for the window to play.
    /// </summary>
    public string? Remove(int index)
    {
        if (!IsFile(index))
        {
            return null;
        }

        var name = Path.GetFileName(_paths[index]);
        _paths.RemoveAt(index);
        if (index < Playing || Playing >= _paths.Count)
        {
            Playing = index < Playing ? Playing - 1 : -1;
        }

        return name;
    }

    /// <summary>The list's lines: the files by name, the playing one marked while <paramref name="active"/>; the one empty line when there is nothing.</summary>
    public IReadOnlyList<string> Lines(bool active)
    {
        if (_paths.Count == 0)
        {
            return [EmptyText];
        }

        var lines = new List<string>(_paths.Count);
        for (var i = 0; i < _paths.Count; i++)
        {
            lines.Add(Path.GetFileName(_paths[i]) + (active && i == Playing ? PlayingMark : string.Empty));
        }

        return lines;
    }

    /// <summary>"Queue, 12 items", "Queue, 1 item", "Queue": the list's accessible name (AM-2.1).</summary>
    public string ListName => _paths.Count switch
    {
        0 => "Queue",
        1 => "Queue, 1 item",
        _ => $"Queue, {_paths.Count} items",
    };

    /// <summary>Ctrl+I on a queued file: its name and place, "03 Chapter 3.mp3, 3 of 12".</summary>
    public string Information(int index) => $"{Path.GetFileName(_paths[index])}, {index + 1} of {_paths.Count}";

    /// <summary>The status bar's right text (AM-2.2): "3 of 12, speed 1.25"; empty with nothing playing.</summary>
    public string PlaceText(double speed) =>
        Playing < 0 ? string.Empty : $"{Playing + 1} of {_paths.Count}, {Axit.Core.MediaFiles.SpeedText(speed).ToLowerInvariant()}";
}
