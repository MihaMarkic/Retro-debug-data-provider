using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class MacDependent: NonWindowsDependent, IOSDependent
{
    public string FileAppOpenName => "open";
}