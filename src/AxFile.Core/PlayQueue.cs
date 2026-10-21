namespace AxFile.Core;

/// <summary>
/// What the pane's list holds (SPEC.md AF-9.2, AF-9.3): the queue of media files, the one playing marked, and
/// under it the operations' status lines, each replaced as its operation goes on and ends, the last twenty kept.
/// The window plays from <see cref="Playing"/> and fills its list from <see cref="Lines"/>. The queue holds paths,
/// not entries: the list may show another folder by the time a file ends (AF-D6).
/// </summary>
public sealed class PlayQueue
{
    public const string EmptyText = "Queue is empty";
    public const string PlayingMark = " (playing)";
    public const int StatusLimit = 20;

    private readonly List<string> _paths = [];
    private readonly List<(int Token, string Text)> _status = [];
    private int _tokens;

    public IReadOnlyList<string> Paths => _paths;

    public int Count => _paths.Count;

    /// <summary>The index of the file playing, or the last one played; -1 with none.</summary>
    public int Playing { get; private set; } = -1;

    public string? PlayingPath => Playing >= 0 && Playing < _paths.Count ? _paths[Playing] : null;

    public string? PlayingName => PlayingPath is { } path ? Path.GetFileName(path) : null;

    /// <summary>The status lines' texts, oldest first.</summary>
    public IReadOnlyList<string> Status => _status.Select(line => line.Text).ToList();

    /// <summary>The queue replaced by <paramref name="paths"/> (Play, Play folder), playing from <paramref name="start"/>.</summary>
    public void Set(IReadOnlyList<string> paths, int start)
    {
        _paths.Clear();
        _paths.AddRange(paths);
        Playing = start >= 0 && start < _paths.Count ? start : -1;
    }

    /// <summary>Add to queue: the paths appended; how many were.</summary>
    public int Add(IEnumerable<string> paths)
    {
        var before = _paths.Count;
        _paths.AddRange(paths);
        return _paths.Count - before;
    }

    /// <summary>Clear queue: the files and the status lines gone.</summary>
    public void Clear()
    {
        _paths.Clear();
        _status.Clear();
        Playing = -1;
    }

    /// <summary>Whether the line at <paramref name="index"/> of <see cref="Lines"/> is a queued file rather than a status line or the empty text.</summary>
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
    /// The line at <paramref name="index"/> taken out (Delete in the pane): a queued file, or a status line; the text
    /// removed, or null when the index is no line. The playing index follows its file; when the file playing is the
    /// one removed, the index stays and now names the file after it, or -1 past the end, for the window to play.
    /// </summary>
    public string? Remove(int index)
    {
        if (IsFile(index))
        {
            var name = Path.GetFileName(_paths[index]);
            _paths.RemoveAt(index);
            if (index < Playing || Playing >= _paths.Count)
            {
                Playing = index < Playing ? Playing - 1 : -1;
            }

            return name;
        }

        var status = index - _paths.Count;
        if (status >= 0 && status < _status.Count)
        {
            var text = _status[status].Text;
            _status.RemoveAt(status);
            return text;
        }

        return null;
    }

    /// <summary>
    /// An operation's status line (AF-9.3): a new line for <paramref name="token"/> -1, or the line that token had
    /// replaced ("Copying 3 items" becoming "Copied 3 items"); the token to report with next. Beyond twenty lines
    /// the oldest go.
    /// </summary>
    public int Report(int token, string text)
    {
        var index = _status.FindIndex(line => line.Token == token);
        if (token >= 0 && index >= 0)
        {
            _status[index] = (token, text);
            return token;
        }

        token = _tokens++;
        _status.Add((token, text));
        while (_status.Count > StatusLimit)
        {
            _status.RemoveAt(0);
        }

        return token;
    }

    /// <summary>The list's lines: the files by name, the playing one marked while <paramref name="active"/>, then the status lines; the one empty line when there is nothing.</summary>
    public IReadOnlyList<string> Lines(bool active)
    {
        var lines = new List<string>(_paths.Count + _status.Count);
        for (var i = 0; i < _paths.Count; i++)
        {
            lines.Add(Path.GetFileName(_paths[i]) + (active && i == Playing ? PlayingMark : string.Empty));
        }

        lines.AddRange(_status.Select(line => line.Text));
        if (lines.Count == 0)
        {
            lines.Add(EmptyText);
        }

        return lines;
    }

    /// <summary>Ctrl+I on a queued file: its name and place, "03 Chapter 3.mp3, 3 of 12".</summary>
    public string Information(int index) => $"{Path.GetFileName(_paths[index])}, {index + 1} of {_paths.Count}";
}
