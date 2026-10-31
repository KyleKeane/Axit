namespace Axit.Forms;

/// <summary>
/// The check box of every app (docs/axit/SPEC.md AX-8.8, 2026-10-31): a <c>CheckBox</c> that says its new state,
/// "checked" or "not checked", when the user toggles it. NVDA read nothing on Space in the consent notice's box, so the
/// state is one announcement of its own; a change the window makes while the box has no focus says nothing.
/// <c>tests/Axit.Tests/CheckBoxTests</c> fails on a plain <c>CheckBox</c> anywhere else.
/// </summary>
public sealed class AxitCheckBox : CheckBox
{
    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        if (Focused)
        {
            Announcer.Raise(this, Checked ? "checked" : "not checked", true);
        }
    }
}
