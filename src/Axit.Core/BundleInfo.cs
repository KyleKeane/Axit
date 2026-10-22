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

    /// <summary>
    /// A Debug build, the one <c>run.ps1</c> makes from the repository: it does not offer the latest release at start
    /// (AX-3.1), since its version is whatever the working tree said when it was last built.
    /// </summary>
#if DEBUG
    public static bool IsDevelopmentBuild { get; } = true;
#else
    public static bool IsDevelopmentBuild { get; } = false;
#endif
}
