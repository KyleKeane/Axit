namespace Axit.Forms;

/// <summary>
/// One view of a window's one list (docs/axgit/SPEC.md AG-2.2, docs/axprocess/SPEC.md PR-2.2; from axGIT on its
/// second use, axProcess, 2026-10-24): the list's name with its count, its items and their keys (a file's path, a
/// commit's hash, a process's id), whether it has been read since it was last made stale, and the key of the item
/// selected in it, kept while the window is open. A window holds one per view and fills its <see cref="AxitList"/>
/// from the one shown.
/// </summary>
public sealed class ListViewState
{
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<string> Labels { get; private set; } = [];

    public IReadOnlyList<string> Keys { get; private set; } = [];

    /// <summary>Whether the view's list is current: read, and not made stale since by a change the window knows of.</summary>
    public bool Loaded { get; set; }

    /// <summary>The key of the item selected in the view, taken before the list changes under it; null before one was chosen.</summary>
    public string? SelectedKey { get; set; }

    public void Set(string name, IEnumerable<string> labels, IEnumerable<string> keys)
    {
        Name = name;
        Labels = labels.ToList();
        Keys = keys.ToList();
        Loaded = true;
    }

    /// <summary>The key of the item at <paramref name="index"/>; null out of range (nothing selected).</summary>
    public string? KeyAt(int index) => index >= 0 && index < Keys.Count ? Keys[index] : null;

    /// <summary>The key of the list's selected item kept, before the list changes under it (a refresh, a view switch); the key kept stays when nothing is selected.</summary>
    public void Remember(ListBox list) => SelectedKey = KeyAt(list.SelectedIndex) ?? SelectedKey;

    /// <summary>The list's items for this view, named with the view and the count, the selection on the kept key when it is still listed, else the first item.</summary>
    public void FillInto(ListBox list)
    {
        list.BeginUpdate();
        list.Items.Clear();
        list.Items.AddRange(Labels.Select(label => (object)label).ToArray());
        list.EndUpdate();
        list.AccessibleName = Name;
        var index = -1;
        for (var i = 0; SelectedKey is { } key && i < Keys.Count && index < 0; i++)
        {
            if (Keys[i] == key)
            {
                index = i;
            }
        }

        if (list.Items.Count > 0)
        {
            list.SelectedIndex = index >= 0 ? index : 0;
        }
    }
}
