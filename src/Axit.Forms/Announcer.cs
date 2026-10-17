using System.Windows.Forms.Automation;

namespace Axit.Forms;

/// <summary>
/// One UI Automation notification on a control (AX-7.1): NVDA speaks it without a focus change. "Most recent" cancels
/// what is queued, for the result of the user's own key; "all" queues behind it, for status. False when the control has
/// no handle yet, so that the caller can fall back to a selection.
/// </summary>
public static class Announcer
{
    public static bool Raise(Control source, string text, bool interrupt)
    {
        if (!source.IsHandleCreated)
        {
            return false;
        }

        var processing = interrupt ? AutomationNotificationProcessing.MostRecent : AutomationNotificationProcessing.All;
        return source.AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.ActionCompleted, processing, text);
    }
}
