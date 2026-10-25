namespace Axit.TestShared;

/// <summary>
/// A fresh folder under the temporary path for one test or one test class ("&lt;prefix&gt;-&lt;guid&gt;"), made at once and
/// deleted with everything in it on Dispose. Compiled into every test project through tests/Directory.Build.props.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder(string prefix)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The folder's full path.</summary>
    public string Path { get; }

    /// <summary>Writes the bytes to the file of that name in the folder (a name with folders in it makes them) and returns its path.</summary>
    public string File(string name, byte[] bytes)
    {
        var path = Prepare(name);
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>Writes the text, as UTF-8 without a byte order mark, to the file of that name in the folder and returns its path.</summary>
    public string File(string name, string text)
    {
        var path = Prepare(name);
        System.IO.File.WriteAllText(path, text);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A program a test started can hold the folder for a moment; Windows cleans the temporary folder.
        }
    }

    private string Prepare(string name)
    {
        var path = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        return path;
    }
}
