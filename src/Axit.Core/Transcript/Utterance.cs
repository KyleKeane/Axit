using System.Text;

namespace Axit.Core.Transcript;

/// <summary>
/// How lines join into one notification (D32): a row wrapped under the previous one joins with a space; any
/// other line with a full stop, unless the text so far ends with a pause of its own, so that a heading or a list
/// item is heard as one. Shared by the spoken replies (<see cref="Arrivals"/>) and by the keys that read a kind of
/// line to the end of the conversation (FR-5).
/// </summary>
public static class Utterance
{
    private static readonly char[] PauseEndings = ['.', ',', ';', ':', '!', '?', '…'];

    public static void Append(StringBuilder text, string next, bool joinedToPrevious)
    {
        if (text.Length > 0)
        {
            if (joinedToPrevious)
            {
                if (!next.StartsWith(' '))
                {
                    text.Append(' ');
                }
            }
            else
            {
                text.Append(Array.IndexOf(PauseEndings, text[^1]) >= 0 ? " " : ". ");
            }
        }

        text.Append(next);
    }
}
