using Axit.Core;

namespace Axit.Tests;

/// <summary>The European letters taken apart and put together without the globalization data (Accents; TextMatch AX-10.3, AxFile's lookalike names).</summary>
public sealed class AccentsTests
{
    [Fact]
    public void Every_row_pairs_each_letter_with_one_base()
    {
        foreach (var (mark, letters, bases) in Accents.Rows)
        {
            Assert.True(letters.Length == bases.Length, $"mark U+{(int)mark:X4}: {letters.Length} letters, {bases.Length} bases");
        }

        Assert.Equal(Accents.PlainRow.Letters.Length, Accents.PlainRow.Bases.Length);
        var all = Accents.Rows.SelectMany(row => row.Letters).Concat(Accents.PlainRow.Letters).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void A_letter_splits_into_its_base_and_mark_and_composes_back()
    {
        Assert.Equal(('e', '́'), Accents.Split('é'));
        Assert.Equal(('E', '̌'), Accents.Split('Ě'));
        Assert.Equal(('u', '̊'), Accents.Split('ů'));
        Assert.Null(Accents.Split('e'));
        Assert.Null(Accents.Split('Ł'));
        Assert.Equal('é', Accents.Compose('e', '́'));
        Assert.Equal('Ñ', Accents.Compose('N', '̃'));
        Assert.Null(Accents.Compose('x', '́'));
    }

    [Fact]
    public void Stripping_leaves_the_plain_letter()
    {
        Assert.Equal('e', Accents.Strip('é'));
        Assert.Equal('L', Accents.Strip('Ł'));
        Assert.Equal('s', Accents.Strip('ß'));
        Assert.Equal('a', Accents.Strip('a'));
        Assert.Equal('ж', Accents.Strip('ж'));
    }
}
