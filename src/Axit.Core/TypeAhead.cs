namespace Axit.Core;

/// <summary>
/// Where letters typed in a list jump (docs/axit/SPEC.md AX-8.6; docs/axfile/SPEC.md AF-3.6; from AxFile.Core's
/// <c>FolderListing</c> on its second use, axProcess, 2026-10-24), as File Explorer takes them: the first name that
/// starts with the typed text, searched from the item in hand and round the end. A run of one letter ("a", "aa")
/// cycles instead through the names starting with that letter, from the next item on, while any other prefix is
/// searched whole, so that a name with a doubled letter inside it ("aab") is found. Backward (Shift with the letter,
/// the user's ask, 2026-10-24) is the same search the other way: the cycle from the item before, a prefix from the
/// item in hand up to the first and round the end. The case of the letters never matters.
/// </summary>
public static class TypeAhead
{
    /// <summary>The index the typed text jumps to; -1 when no name starts with it.</summary>
    public static int Find(IReadOnlyList<string> names, string typed, int current, bool backward = false)
    {
        if (names.Count == 0 || typed.Length == 0)
        {
            return -1;
        }

        var cycling = typed.All(letter => char.ToUpperInvariant(letter) == char.ToUpperInvariant(typed[0]));
        var prefix = cycling ? typed[..1] : typed;
        var direction = backward ? -1 : 1;
        var from = Math.Clamp(current, 0, names.Count - 1) + (cycling ? direction : 0);
        for (var step = 0; step < names.Count; step++)
        {
            var i = ((from + step * direction) % names.Count + names.Count) % names.Count;
            if (names[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
