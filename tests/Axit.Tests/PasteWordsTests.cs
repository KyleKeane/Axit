using Axit.Forms;

namespace Axit.Tests;

/// <summary>What a paste into a text says (docs/axit/SPEC.md AX-7.3, 2026-10-25): never the text itself.</summary>
public class PasteWordsTests
{
    [Fact]
    public void One_line_is_pasted_and_several_say_how_many()
    {
        Assert.Equal("Pasted", AxitForm.PasteWords("hello", multiline: true));
        Assert.Equal("Pasted, 3 lines", AxitForm.PasteWords("one\r\ntwo\r\nthree\r\n", multiline: true));
        Assert.Equal("Pasted, 2 lines", AxitForm.PasteWords("one\ntwo", multiline: true));
    }

    [Fact]
    public void A_one_line_field_says_pasted_and_no_text_says_so()
    {
        // A field of one line keeps the first line only, so the count would be wrong there.
        Assert.Equal("Pasted", AxitForm.PasteWords("one\r\ntwo", multiline: false));
        Assert.Equal("Nothing to paste as text", AxitForm.PasteWords(null, multiline: true));
    }
}
