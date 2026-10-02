namespace Axit.Forms;

/// <summary>
/// NVDA finds a status bar by looking at the bottom-left pixel of the foreground window's rectangle. It takes that
/// rectangle from UI Automation, and for a WinForms window that is the full window rectangle including the invisible
/// resize border, so the pixel lies outside the visible window and NVDA+End reports no status bar. Reporting the
/// client area (with the title bar) instead puts that pixel in the status strip. Classic Win32 windows report their
/// client area through MSAA, which is why the trick is not needed there. Every window of the bundle returns one of
/// these from <c>CreateAccessibilityInstance</c>.
/// </summary>
public sealed class WindowAccessibleObject(Form owner) : Control.ControlAccessibleObject(owner)
{
    public override Rectangle Bounds
    {
        get
        {
            if (!owner.IsHandleCreated)
            {
                return Rectangle.Empty;
            }

            var client = owner.RectangleToScreen(owner.ClientRectangle);
            var top = Math.Min(owner.Bounds.Top, client.Top);
            return Rectangle.FromLTRB(client.Left, top, client.Right, client.Bottom);
        }
    }
}
