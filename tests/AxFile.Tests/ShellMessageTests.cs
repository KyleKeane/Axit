using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The shell's codes for a file operation as one line each (docs/axfile/SPEC.md AF-4.5), with Windows' own message for the low ones and a code for the rest.</summary>
public sealed class ShellMessageTests
{
    [Theory]
    [InlineData(0x20, "In use by another program: a terminal or an app runs in it, or the file is open")]
    [InlineData(0x21, "In use by another program: a terminal or an app runs in it, or the file is open")]
    [InlineData(0x71, "The source and the destination are the same")]
    [InlineData(0x74, "Move of a drive's root is not allowed")]
    [InlineData(0x75, "Move was cancelled")]
    [InlineData(0x76, "A folder cannot go inside itself")]
    [InlineData(0x78, "Access denied")]
    [InlineData(0x7C, "The path is not valid")]
    [InlineData(0x7E, "A file and a folder of that name are in the way")]
    [InlineData(0x80, "A file and a folder of that name are in the way")]
    [InlineData(0x81, "The name is too long")]
    [InlineData(0x402, "Move failed")]
    [InlineData(0x999, "Move failed (code 999)")]
    public void Each_shell_code_has_its_line(int code, string expected)
    {
        Assert.Equal(expected, FileOperations.ShellMessage(code, "Move"));
    }

    [Fact]
    public void A_low_code_is_windows_own_message()
    {
        Assert.Contains("cannot find the file", FileOperations.ShellMessage(2, "Copy"));
        Assert.Equal(FileOperations.InUse, FileOperations.ShellMessage(0x20, "Copy"));
    }
}
