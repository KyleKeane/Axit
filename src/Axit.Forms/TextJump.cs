using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// A window's key pair for one kind of element in a notice's text (docs/axit/SPEC.md AX-7.2): the key for the next one,
/// the key for the previous one, the kind, and for headings the level (0 for any). Each window builds its list from its
/// own key table (<c>AxClaudeKeys.NoticeJumps</c>, <c>AxDownKeys.NoticeJumps</c>) and hands it to
/// <see cref="OverlayPanel.Jumps"/>, so that a notice follows the window's own convention.
/// </summary>
public sealed record TextJump(Keys Next, Keys Previous, ElementKind Kind, int Level = 0);
