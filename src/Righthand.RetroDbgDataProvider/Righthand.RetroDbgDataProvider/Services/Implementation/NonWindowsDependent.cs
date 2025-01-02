using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class NonWindowsDependent : IOsDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCulture;
    public StringComparer FileStringComparer => StringComparer.CurrentCulture;
}