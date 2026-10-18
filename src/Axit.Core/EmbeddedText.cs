using System.Reflection;

namespace Axit.Core;

/// <summary>
/// A text file embedded in an assembly: each app's user guide (Help → User guide) and the bundle's Axit guide
/// (AX-9.9). Read whole, or one line that says where it is in the source when the build left it out.
/// </summary>
public static class EmbeddedText
{
    public static string Read(Assembly assembly, string name, string sourcePath)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return $"{name} is not included in this build. It is {sourcePath} in the source repository.";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
