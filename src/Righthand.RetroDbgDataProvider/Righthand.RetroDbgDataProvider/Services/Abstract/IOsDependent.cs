using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Services.Abstract;

internal interface IOsDependent
{
    StringComparison FileStringComparison { get; }
    StringComparer FileStringComparer { get; }

    FrozenSet<string> ToFileFrozenSet(IList<string> files) => files.ToFrozenSet(FileStringComparer);
}