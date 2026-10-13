namespace AxClaude.Core.Transcript;

/// <summary>Where the app presents a question Claude waits on.</summary>
public enum QuestionDialog
{
    /// <summary>The answer notice: one answer out of a list, or several ticked, chosen with their keys or the arrow keys.</summary>
    AnswerNotice,

    /// <summary>
    /// No dialog of the app's own: the question stays in the conversation and is answered from the message field
    /// (the lists of settings panels the app cannot show yet).
    /// </summary>
    MessageField,
}

/// <summary>
/// Decides how a question Claude waits on is presented (SPEC.md D33): as a list of answers in the control area (check
/// boxes when it takes several), unless the slash command sent last opens more than a list (<see cref="CommandInteraction.Multi"/>,
/// <see cref="CommandInteraction.Viewer"/>, <see cref="CommandInteraction.Panel"/>, docs/axclaude/claude-screens.md),
/// in which case it stays in the message field. The question itself makes no difference.
/// </summary>
public static class QuestionRouter
{
    public static QuestionDialog DialogFor(SlashCommand? lastCommand) =>
        lastCommand is { Interaction: CommandInteraction.Multi or CommandInteraction.Viewer or CommandInteraction.Panel }
            ? QuestionDialog.MessageField
            : QuestionDialog.AnswerNotice;
}
