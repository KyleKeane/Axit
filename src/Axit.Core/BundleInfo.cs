using System.Reflection;

namespace Axit.Core;

/// <summary>
/// The bundle's version: <c>&lt;Version&gt;</c> in <c>src/Axit/Axit.csproj</c>, read from the executable, so every app
/// shows the same number (docs/axit/SPEC.md AX-3.1). "0.0.0" when there is no executable, as in a test host.
/// </summary>
public static class BundleInfo
{
    public static string Version { get; } =
        Assembly.GetEntryAssembly()?.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
}
