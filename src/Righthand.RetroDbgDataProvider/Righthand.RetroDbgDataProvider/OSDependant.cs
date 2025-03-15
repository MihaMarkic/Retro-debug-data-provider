using System.Runtime.InteropServices;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Provides static os dependant methods where DI is not possible.
/// </summary>
public static class OsDependent
{
    /// <inheritdoc cref="IOSDependent"/>
    public static StringComparison FileStringComparison { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.CurrentCultureIgnoreCase
        : StringComparison.CurrentCulture;
    /// <inheritdoc cref="IOSDependent"/>
    public static StringComparer FileStringComparer { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.CurrentCultureIgnoreCase
        : StringComparer.CurrentCulture;
    /// <inheritdoc cref="IOSDependent.NormalizePath"/>
    public static string NormalizePath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return path.Replace("/", "\\");
        }
        else
        {
            return path.Replace("\\", "/");
        }
    }
}