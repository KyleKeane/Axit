namespace AxClip;

/// <summary>
/// axClip's window-specific keys (SPEC.md CP-5.2; bundle AX-8). A key is changed here and nowhere else: the menu items,
/// the key switch in <c>ClipForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys (F1, the
/// palette, text size, Page Up and Page Down) are <c>Axit.Forms.BundleKeys</c>; the hotkey is a setting, not a key of
/// the window. The caret stays in the field: every key here acts on the list of items, as in axStart (AS-D2).
/// </summary>
public static class AxClipKeys
{
    /// <summary>The names are axStart's where the keys do the same (AS-5.3).</summary>
    public const Keys NextMatch = Keys.Down;

    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>The item pasted into the program that was in front (CP-4.1).</summary>
    public const Keys Paste = Keys.Return;

    /// <summary>The item on the clipboard, without pasting (CP-4.2).</summary>
    public const Keys CopyOnly = Keys.Shift | Keys.Return;

    public const Keys ReadInFull = Keys.Control | Keys.Q;
    public const Keys Remove = Keys.Delete;
    public const Keys ClearHistory = Keys.Control | Keys.Shift | Keys.Delete;
    public const Keys PauseRecording = Keys.Control | Keys.P;

    /// <summary>The window hidden at once.</summary>
    public const Keys Hide = Keys.Escape;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Next match", NextMatch),
        ("Previous match", PreviousMatch),
        ("First match", FirstMatch),
        ("Last match", LastMatch),
        ("Paste", Paste),
        ("Copy without pasting", CopyOnly),
        ("Read in full", ReadInFull),
        ("Remove the item", Remove),
        ("Clear the history", ClearHistory),
        ("Pause recording", PauseRecording),
        ("Hide", Hide),
    ];
}
