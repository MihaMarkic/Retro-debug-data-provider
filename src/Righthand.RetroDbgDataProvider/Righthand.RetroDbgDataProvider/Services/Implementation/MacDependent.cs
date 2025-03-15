using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

/// <summary>
/// MacOS dependant code.
/// </summary>
public class MacDependent: NonWindowsDependent, IOSDependent
{
    /// <inheritdoc />
    public string FileAppOpenName => "open";
}