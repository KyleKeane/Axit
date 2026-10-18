using Axit.Forms;
using AxJSON.Core;

namespace AxJSON;

/// <summary>
/// AxJSON's window-specific keys (SPEC.md AJ-2.2, AJ-2.3; bundle AX-8). A key is changed here and nowhere else: the
/// menu items, the key handling and the F1 text read this table. The editor shell's keys (new, open, save as, go to
/// line, word wrap, show in File Explorer) are <c>Axit.Forms.EditorKeys</c>, the same in every editor window, and
/// <see cref="All"/> includes them; the bundle-wide keys are <c>Axit.Forms.BundleKeys</c>. The letters follow AxDown's
/// where the concept exists in both (L a list and an array, I an item in both); the others are JSON's own (AJ-D5).
/// </summary>
public static class AxJSONKeys
{
    public const Keys NextKey = Keys.Control | Keys.K;
    public const Keys PreviousKey = Keys.Control | Keys.Shift | Keys.K;

    /// <summary>J, the braces of JSON: O is Open in every editor window.</summary>
    public const Keys NextObject = Keys.Control | Keys.J;

    public const Keys PreviousObject = Keys.Control | Keys.Shift | Keys.J;
    public const Keys NextArray = Keys.Control | Keys.L;
    public const Keys PreviousArray = Keys.Control | Keys.Shift | Keys.L;
    public const Keys NextItem = Keys.Control | Keys.I;
    public const Keys PreviousItem = Keys.Control | Keys.Shift | Keys.I;
    public const Keys NextError = Keys.Control | Keys.E;
    public const Keys PreviousError = Keys.Control | Keys.Shift | Keys.E;

    /// <summary>Edit → Format (AJ-2.3).</summary>
    public const Keys Format = Keys.Control | Keys.Shift | Keys.F;

    /// <summary>Every key of the window with its name, the editor shell's first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ..EditorKeys.All,
        ("Next key", NextKey),
        ("Previous key", PreviousKey),
        ("Next object", NextObject),
        ("Previous object", PreviousObject),
        ("Next array", NextArray),
        ("Previous array", PreviousArray),
        ("Next item", NextItem),
        ("Previous item", PreviousItem),
        ("Next error", NextError),
        ("Previous error", PreviousError),
        ("Format", Format),
    ];

    /// <summary>The keys of AJ-2.2 as pairs, one per kind, with the outline's functions: the editor and every notice text take this list (bundle AX-7.2).</summary>
    public static readonly IReadOnlyList<TextJump> Jumps =
    [
        new(NextKey, PreviousKey, "key", JsonOutline.NextKey, JsonOutline.PreviousKey),
        new(NextObject, PreviousObject, "object", JsonOutline.NextObject, JsonOutline.PreviousObject),
        new(NextArray, PreviousArray, "array", JsonOutline.NextArray, JsonOutline.PreviousArray),
        new(NextItem, PreviousItem, "item", JsonOutline.NextItem, JsonOutline.PreviousItem),
        new(NextError, PreviousError, "error", JsonOutline.NextError, JsonOutline.PreviousError),
    ];
}
