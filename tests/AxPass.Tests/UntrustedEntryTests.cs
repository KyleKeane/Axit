using AxPass.Core;

namespace AxPass.Tests;

/// <summary>What a database from someone else can put in an entry, and what axPass does with it (security audit S-4, S-6).</summary>
public sealed class UntrustedEntryTests
{
    [Theory]
    [InlineData("https://example.com/login", "https://example.com/login")]
    [InlineData("http://example.com", "http://example.com/")]
    [InlineData("example.com/login", "https://example.com/login")]
    [InlineData(@"\\server\share\x.exe", null)]
    [InlineData(@"C:\Windows\System32\calc.exe", null)]
    [InlineData("file:///C:/x.lnk", null)]
    [InlineData("search-ms:query=x&crumb=location:\\\\server\\s", null)]
    [InlineData("mailto:someone@example.invalid", null)]
    [InlineData("just words", null)]
    public void Only_a_web_address_is_opened(string url, string? opened)
    {
        Assert.Equal(opened, EntryFields.WebAddress(url));
    }

    [Fact]
    public void A_name_that_looks_like_an_option_reaches_the_tool_as_a_path()
    {
        Assert.Equal(["show", "-q", "db.kdbx", "/--key-file=x"], KeePassCli.On("show", "db.kdbx", null, "--key-file=x"));
        Assert.Equal(["show", "-q", "db.kdbx", "Email/Gmail"], KeePassCli.On("show", "db.kdbx", null, "Email/Gmail"));
    }
}
