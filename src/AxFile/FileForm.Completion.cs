using System.Diagnostics.CodeAnalysis;
using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>axFile's window, the completion in the Go to folder field (AF-3.3, AF-D12): Down and Up complete the folder through AxFile.Core.PathCompletion.</summary>
public sealed partial class FileForm
{
    // ---- completion in the Go to folder field (AF-3.3, AF-D12) ----

    /// <summary>The completions of the text last completed from, in the list's order.</summary>
    private IReadOnlyList<Completion> _completions = [];

    /// <summary>The completion the field holds; -1 before the first.</summary>
    private int _completionAt = -1;

    /// <summary>The text the last completion put in the field: while the field still holds it, Down and Up move among the same completions; anything typed starts afresh.</summary>
    private string? _completed;

    /// <summary>Counts the folder reads, so that a slow read that comes back after a newer one is dropped.</summary>
    private int _completionReads;

    /// <summary>Whether the field completes folders alone (Move to folder, AF-4.13) or every entry (Go to folder); set by the notice that opens.</summary>
    private bool _completeFoldersOnly;

    /// <summary>A notice's path field starts with no completion in hand.</summary>
    private void ResetCompletion(bool foldersOnly)
    {
        _completed = null;
        _completions = [];
        _completeFoldersOnly = foldersOnly;
    }

    /// <summary>The Go to folder field's keys: Down and Up the next and the previous completion, Right at the end of a completed folder its first entry.</summary>
    private bool OnPathKey(Keys key)
    {
        switch (key)
        {
            case AxFileKeys.CompleteNext:
                Do(() => CompleteAsync(1));
                return true;
            case AxFileKeys.CompletePrevious:
                Do(() => CompleteAsync(-1));
                return true;
            case AxFileKeys.IntoFolder when _completed is { } done && done.EndsWith('\\') && Overlay.InputText == done && Overlay.InputCaret == done.Length:
                // The folder's own entries next: the text is now what the completion starts from.
                _completed = null;
                Do(() => CompleteAsync(1));
                return true;
        }

        return false;
    }

    /// <summary>
    /// The next or the previous completion in the field, "Documents, folder, 2 of 5"; the folder the text names is read
    /// on a task when the text is new; past either end the edge earcon and then the completion again, as the palette
    /// and axStart do at an end (AX-10.4); the earcon alone when no name starts with the text.
    /// </summary>
    private async Task CompleteAsync(int step)
    {
        var typed = Overlay.InputText;
        if (typed != _completed)
        {
            var (folder, start) = PathCompletion.Split(typed, _folder);
            var read = ++_completionReads;
            var (hidden, dot) = (_settings.ShowHidden, _settings.ShowDotFiles);
            var foldersOnly = _completeFoldersOnly;
            var found = await Task.Run(() => PathCompletion.Candidates(folder, start, hidden, dot)
                .Where(candidate => !foldersOnly || candidate.Text.EndsWith('\\')).ToList());
            if (read != _completionReads || !NoticeOpen || Overlay.InputText != typed)
            {
                return;
            }

            _completions = found;
            _completionAt = step > 0 ? -1 : found.Count;
        }

        var next = _completionAt + step;
        if (next < 0 || next >= _completions.Count)
        {
            Sounds.Edge();
            if (_completionAt >= 0 && _completionAt < _completions.Count)
            {
                Announce(PathCompletion.Spoken(_completions[_completionAt], _completionAt, _completions.Count), true);
            }

            return;
        }

        _completionAt = next;
        var completion = _completions[next];
        _completed = completion.Text;
        Overlay.SetInputText(completion.Text);
        Announce(PathCompletion.Spoken(completion, next, _completions.Count), true);
    }

    /// <summary>The field's first text: the folder shown with a backslash at its end, so that the first Down completes among its own entries; empty at This PC.</summary>
    private string PathToComplete() =>
        RealFolder is not { } folder ? string.Empty : folder.EndsWith('\\') ? folder : folder + "\\";

    /// <summary>Folder → Go to folder… (Ctrl+G): a typed path, environment variables allowed, a file's path selecting the file, with completion on Down and Up (AF-3.3).</summary>
    private void GoToFolder()
    {
        ResetCompletion(foldersOnly: false);
        ShowNotice(new Notice("Go to folder", "A folder's path, or a file's to select it in its folder. %USERPROFILE%\\Downloads and the other variables work. Down and Up complete the path from what is typed; Right goes into a completed folder.",
        [
            new OverlayChoice("&Go", () =>
            {
                var typed = Overlay.InputText;
                if (FolderListing.Resolve(typed) is not { } target)
                {
                    Announce("Not found: " + typed.Trim(), true);
                    return;
                }

                Do(() => ShowFolderAsync(target.Folder, target.File is { } file ? Path.GetFileName(file) : null, remember: true));
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Path:", "Path", PathToComplete(), "Type a path", KeyHook: OnPathKey),
        });
    }

    /// <summary>Folder → Refresh (F5): the folder read again, the selection kept by name (AF-3.4).</summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (await ShowFolderAsync(_folder, CurrentEntry()?.Name, remember: false) && announce)
        {
            Announce("Refreshed", true);
        }
    }

    /// <summary>Coming back to the window (AF-3.4): the folder read again, silently, when its write time moved; nothing otherwise.</summary>
    private async Task RefreshIfChangedAsync()
    {
        if (_folder is { } folder && !NoticeOpen && Stamp(folder) != _folderStamp)
        {
            await ShowFolderAsync(folder, CurrentEntry()?.Name, remember: false);
        }
    }
}
