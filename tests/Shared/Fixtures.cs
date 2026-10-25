namespace Axit.TestShared;

/// <summary>The recorded fixtures under tests/fixtures. Compiled into every test project through tests/Directory.Build.props.</summary>
internal static class Fixtures
{
    /// <summary>The tests/fixtures folder, found above the test binary.</summary>
    public static string Folder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("tests/fixtures was not found above " + AppContext.BaseDirectory);
    }
}
