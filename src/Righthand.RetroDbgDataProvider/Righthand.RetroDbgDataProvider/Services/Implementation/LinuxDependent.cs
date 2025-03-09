using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class LinuxDependent: NonWindowsDependent, IOSDependent
{
    public string FileAppOpenName => "xdg-open";
}