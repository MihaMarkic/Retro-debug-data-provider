using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Services.Implementation;

public class WindowsDependent : IOsDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCultureIgnoreCase;
    public StringComparer FileStringComparer => StringComparer.CurrentCultureIgnoreCase;
}