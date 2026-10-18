namespace AxClaude.Core.Transcript;

/// <summary>
/// When a feedback draft Claude Code queued by itself opens /feedback (SPEC.md D33). Claude counts its drafts on the
/// mode line ("1 feedback draft", <see cref="SessionModel.FeedbackDrafts"/>); the count is compared with the one before
/// the turn that ran last, so that a draft made in this session's turn opens, while drafts counted at startup or left
/// by other sessions wait for /feedback by hand (they opened the list on every start before 2.10.1).
/// </summary>
public sealed class FeedbackDraftWatch
{
    /// <summary>The title of Claude's list of drafts, which the app answers itself and closes again (D33).</summary>
    public const string ListTitle = "Feedback drafts";

    private int _before;
    private bool _worked;

    /// <summary>
    /// Called at the end of every frame once Claude is ready, with whether it works, whether it is idle (at its
    /// prompt, nothing on its way to it) and the count of its mode line. True while a draft made in the turn that just
    /// ended waits: /feedback should open on it, after which <see cref="Opened"/> takes the count. An idle count
    /// without a turn before it (startup, a draft sent or discarded, a turn that drafted nothing) is the one the next
    /// turn is measured against.
    /// </summary>
    public bool Drafted(bool working, bool idle, int drafts)
    {
        if (working)
        {
            _worked = true;
            return false;
        }

        if (!idle)
        {
            return false;
        }

        if (_worked && drafts > _before)
        {
            return true;
        }

        _before = drafts;
        _worked = false;
        return false;
    }

    /// <summary>/feedback opened for the draft, or the draft was left where it is: the next turn is measured against this count.</summary>
    public void Opened(int drafts)
    {
        _before = drafts;
        _worked = false;
    }

    /// <summary>
    /// The answer on Claude's list of drafts that is the draft titled <paramref name="title"/> (the card's "Product
    /// feedback drafted: &lt;title&gt;"): its text is the title, a comma and the draft's type ("Fixture card test, idea ·
    /// 1m · …"). Null when no answer is, in which case the list is shown as it is.
    /// </summary>
    public static QuestionOption? Find(Question question, string title) =>
        question.Title == ListTitle
            ? question.Options.FirstOrDefault(option => option.Text.StartsWith(title + ",", StringComparison.Ordinal))
            : null;
}
