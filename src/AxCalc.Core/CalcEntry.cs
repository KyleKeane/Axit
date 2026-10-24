namespace AxCalc.Core;

/// <summary>
/// One line of the history (SPEC.md AC-5.1): the expression as typed, the answer's text and value, and the variable an
/// assignment set. Saved in the settings file as it is (AC-9.1).
/// </summary>
public sealed record CalcEntry(string Expression, string Answer, double Value, string? Name = null)
{
    /// <summary>"1920*1080*4 = 8294400", or "rate = 1.25" for an assignment.</summary>
    public string Label => Name is null ? $"{Expression} = {Answer}" : $"{Name} = {Answer}";

    /// <summary>The entry for an answered expression; the expression without the spaces around it.</summary>
    public static CalcEntry From(string expression, Evaluation answer) => new(expression.Trim(), answer.Answer, answer.Value, answer.Name);
}
