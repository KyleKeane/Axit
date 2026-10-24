namespace Axit.Core;

/// <summary>
/// Where letters typed in a list jump (docs/axfile/SPEC.md AF-3.6; from AxFile.Core's <c>FolderListing</c> on its second
/// use, axProcess, 2026-10-24), as File Explorer takes them: the first name that starts with <c>typed</c>, searched from
/// the item in hand and round the end. A run of one letter ("a", "aa") cycles instead through the names starting with
/// that letter, from the next item on, while any other prefix is searched whole, so that a name with a doubled letter
/// inside it ("aab") is found.
/// </summary>
public static class TypeAhead
{
    /// <summary>The index the typed text jumps to; -1 when no name starts with it.</summary>
    public static int Find(IReadOnlyList<string> names, string typed, int current)
    {
        if (names.Count == 0 || typed.Length == 0)
        {
            return -1;
        }

        var cycling = typed.All(letter => char.ToUpperInvariant(letter) == char.ToUpperInvariant(typed[0]));
        var prefix = cycling ? typed[..1] : typed;
        var from = Math.Clamp(current, 0, names.Count - 1) + (cycling ? 1 : 0);
        for (var step = 0; step < names.Count; step++)
        {
            var i = (from + step) % names.Count;
            if (names[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
