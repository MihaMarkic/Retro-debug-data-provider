using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

/// <summary>
/// Linux dependent code.
/// </summary>
public class LinuxDependent: NonWindowsDependent, IOSDependent
{
    /// <inheritdoc />
    public string FileAppOpenName => "xdg-open";
}