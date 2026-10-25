using Axit.Core;

namespace Axit.Tests;

public class FileSizeTests
{
    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1, "1 byte")]
    [InlineData(512, "512 bytes")]
    [InlineData(12 * 1024, "12 KB")]
    [InlineData(1258291, "1.2 MB")]
    [InlineData(3758096384, "3.5 GB")]
    public void Sizes_read_as_people_say_them(long bytes, string expected)
    {
        Assert.Equal(expected, FileSize.Text(bytes));
    }
}
