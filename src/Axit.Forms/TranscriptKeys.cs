using Axit.Core.Transcript;

namespace Axit.Forms;

/// <summary>
/// A single-key jump target of a <see cref="TranscriptView"/> (AxClaude FR-5): the letter, the kind's spoken name
/// ("No next input"), which lines it stops at and, when given, a rule about the line before the target that must
/// hold too (the first line always qualifies). <paramref name="ReadsWhole"/> marks the paragraph key, whose
/// read-to-the-end takes each paragraph's following lines as well (AxClaude D36).
/// </summary>
public sealed record QuickKey(Keys Key, string Name, Func<Line, bool> Matches, Func<Line, Line, bool>? Previous = null, bool ReadsWhole = false);

/// <summary>
/// The keys of a transcript view, handed in by the window from its own key table (docs/axit/SPEC.md AX-8.3), so that
/// the view is the bundle's and the letters stay each window's: the quick keys (the bookmark and the response keys
/// among them), the bookmark toggle (m), the line number (l), the way back from a jump (Backspace), Enter on a line,
/// Escape and the copy of a selection; and the two keys the console window handles from anywhere, the bookmark from
/// outside the view (Ctrl+Shift+K) and the modifiers that read a kind of line to the end (Ctrl+Alt, AxClaude D36).
/// </summary>
public sealed record TranscriptKeys(
    IReadOnlyList<QuickKey> QuickKeys,
    QuickKey Bookmark,
    QuickKey Response,
    Keys BookmarkToggle,
    Keys LineNumber,
    Keys JumpBack,
    Keys Enter,
    Keys Escape,
    Keys CopySelection,
    Keys BookmarkLine,
    Keys ReadToEnd);
