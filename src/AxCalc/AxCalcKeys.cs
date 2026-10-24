namespace AxCalc;

/// <summary>
/// axCalc's window-specific keys (SPEC.md AC-7.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>CalcForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter, Escape and
/// Delete mean what the control with the focus needs: the field answers and clears, the history recalls and removes.
/// </summary>
public static class AxCalcKeys
{
    /// <summary>In the field, the answer (AC-6.1); in the history, the entry's expression back in the field (AC-5.2).</summary>
    public const Keys Act = Keys.Return;

    /// <summary>In the field, the text cleared; in the history, back to the field.</summary>
    public const Keys Back = Keys.Escape;

    /// <summary>In the history only: the entry removed.</summary>
    public const Keys Remove = Keys.Delete;

    public const Keys CopyAnswer = Keys.Control | Keys.Shift | Keys.C;
    public const Keys OtherBases = Keys.Control | Keys.I;
    public const Keys LastAnswer = Keys.Control | Keys.Q;
    public const Keys ClearHistory = Keys.Control | Keys.Shift | Keys.Delete;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Answer (in the field), or recall the entry (in the history)", Act),
        ("Clear the field, or back to the field from the history", Back),
        ("Remove the entry (in the history)", Remove),
        ("Copy the answer", CopyAnswer),
        ("The answer in other bases", OtherBases),
        ("Say the last answer", LastAnswer),
        ("Clear the history", ClearHistory),
    ];
}
