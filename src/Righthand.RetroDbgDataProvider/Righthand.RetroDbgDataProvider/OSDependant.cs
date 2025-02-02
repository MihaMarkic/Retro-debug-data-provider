using System.Runtime.InteropServices;

namespace Righthand.RetroDbgDataProvider;

public static class OsDependent
{
    public static StringComparison FileStringComparison { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.CurrentCultureIgnoreCase
        : StringComparison.CurrentCulture;

    public static StringComparer FileStringComparer { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.CurrentCultureIgnoreCase
        : StringComparer.CurrentCulture;

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