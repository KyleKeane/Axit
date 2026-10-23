namespace Axit.Core;

/// <summary>
/// The accented letters of Latin-1 Supplement and Latin Extended-A (U+00C0 to U+017F), the letters of the European
/// languages, taken apart and put together without the globalization data: the executable runs with invariant
/// globalization (src/Axit/Axit.csproj), where <c>string.Normalize</c> returns its input, so the places that fold or
/// compare text do it from these tables (<see cref="TextMatch.Fold"/>; axFile's names that read the same; found
/// 2026-10-23, when the tests were made to run as the program does). Other scripts are left as they are.
/// </summary>
public static class Accents
{
    /// <summary>The letter without its accent: 'e' for 'é', 'L' for 'Ł', the character itself for any other.</summary>
    public static char Strip(char c) =>
        Parts.TryGetValue(c, out var parts) ? parts.Base : Plain.TryGetValue(c, out var plain) ? plain : c;

    /// <summary>A precomposed letter as its base letter and its combining mark, ('e', U+0301) for 'é'; null for any other character.</summary>
    public static (char Base, char Mark)? Split(char c) => Parts.TryGetValue(c, out var parts) ? parts : null;

    /// <summary>The precomposed letter of a base letter and a combining mark, 'é' for ('e', U+0301); null when there is none.</summary>
    public static char? Compose(char baseLetter, char mark) => Composed.TryGetValue((baseLetter, mark), out var letter) ? letter : null;

    /// <summary>One row per combining mark: the precomposed letters that carry it, and their base letters in the same order (public for the test that checks the rows).</summary>
    public static readonly IReadOnlyList<(char Mark, string Letters, string Bases)> Rows =
    [
        ('̀', "ÀÈÌÒÙàèìòù", "AEIOUaeiou"),
        ('́', "ÁÉÍÓÚÝáéíóúýĆćĹĺŃńŔŕŚśŹź", "AEIOUYaeiouyCcLlNnRrSsZz"),
        ('̂', "ÂÊÎÔÛâêîôûĈĉĜĝĤĥĴĵŜŝŴŵŶŷ", "AEIOUaeiouCcGgHhJjSsWwYy"),
        ('̃', "ÃÑÕãñõĨĩŨũ", "ANOanoIiUu"),
        ('̄', "ĀāĒēĪīŌōŪū", "AaEeIiOoUu"),
        ('̆', "ĂăĔĕĞğĬĭŎŏŬŭ", "AaEeGgIiOoUu"),
        ('̇', "ĊċĖėĠġİŻż", "CcEeGgIZz"),
        ('̈', "ÄËÏÖÜäëïöüÿŸ", "AEIOUaeiouyY"),
        ('̊', "ÅåŮů", "AaUu"),
        ('̋', "ŐőŰű", "OoUu"),
        ('̌', "ČčĎďĚěĽľŇňŘřŠšŤťŽž", "CcDdEeLlNnRrSsTtZz"),
        ('̧', "ÇçĢģĶķĻļŅņŖŗŞşŢţ", "CcGgKkLlNnRrSsTt"),
        ('̨', "ĄąĘęĮįŲų", "AaEeIiUu"),
    ];

    /// <summary>The letters of the two blocks that have no decomposition and still read as a plain letter, with that letter.</summary>
    public static readonly (string Letters, string Bases) PlainRow = ("ÆæÐðØøÞþßĐđĦħıĲĳĸĿŀŁłŊŋŉŒœŦŧſ", "AaDdOoTtsDdHhiIikLlLlNnnOoTts");

    private static readonly Dictionary<char, (char Base, char Mark)> Parts = BuildParts();
    private static readonly Dictionary<(char, char), char> Composed = Parts.ToDictionary(pair => (pair.Value.Base, pair.Value.Mark), pair => pair.Key);
    private static readonly Dictionary<char, char> Plain = PlainRow.Letters.Zip(PlainRow.Bases).ToDictionary(pair => pair.First, pair => pair.Second);

    private static Dictionary<char, (char Base, char Mark)> BuildParts()
    {
        var parts = new Dictionary<char, (char Base, char Mark)>();
        foreach (var (mark, letters, bases) in Rows)
        {
            for (var i = 0; i < letters.Length; i++)
            {
                parts[letters[i]] = (bases[i], mark);
            }
        }

        return parts;
    }
}
