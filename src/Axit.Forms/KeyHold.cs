namespace Axit.Forms;

/// <summary>
/// A hold on the keys that answer (Enter, Space, Escape, typed letters) for a moment after a control appears that the
/// user did not ask for (docs/axit/SPEC.md AX-6.4): a notice (<see cref="OverlayPanel"/>) or a question in the message
/// field's place (<see cref="ControlArea"/>) can open while the user types, and the next keys were meant for the
/// message field; after an answer goes, a second Enter would send it twice. One second unless the caller says
/// otherwise (AxClaude holds for three seconds after an answer). The two panels kept a copy each until 2026-10-23.
/// </summary>
internal sealed class KeyHold
{
    public const int DefaultMs = 1000;

    private long _until;

    /// <summary>The keys do nothing for this long from now.</summary>
    public void Start(int milliseconds = DefaultMs) => _until = Environment.TickCount64 + milliseconds;

    /// <summary>The keys act at once.</summary>
    public void Clear() => _until = 0;

    /// <summary>Whether the keys are held now.</summary>
    public bool Holding => Environment.TickCount64 < _until;
}
