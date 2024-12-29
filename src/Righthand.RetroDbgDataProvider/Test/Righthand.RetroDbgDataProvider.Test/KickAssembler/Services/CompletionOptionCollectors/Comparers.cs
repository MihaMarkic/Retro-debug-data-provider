using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class CompletionOptionComparer: IEqualityComparer<CompletionOption>
{
    public static CompletionOptionComparer Default { get; } = new CompletionOptionComparer();
    public bool Equals(CompletionOption? x, CompletionOption? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return x.RootText == y.RootText && x.ReplacementLength == y.ReplacementLength;
    }

    public int GetHashCode(CompletionOption obj)
    {
        return obj.GetHashCode();
    }

    public static void AddHashCode(CompletionOption source, ref HashCode hc)
    {
        hc.Add(source.RootText);
        hc.Add(source.ReplacementLength);
    }
}
public class ArrayPropertyNameCompletionOptionComparer: IEqualityComparer<ArrayPropertyNameCompletionOption>
{
    public static ArrayPropertyNameCompletionOptionComparer Default { get; } = new ArrayPropertyNameCompletionOptionComparer();
    public bool Equals(ArrayPropertyNameCompletionOption? x, ArrayPropertyNameCompletionOption? y)
    {
        if (!CompletionOptionComparer.Default.Equals(x, y))
        {
            return false;
        }
        return x!.Suggestions.SetEquals(y!.Suggestions);
    }

    public int GetHashCode(ArrayPropertyNameCompletionOption obj)
    {
        var hc = new HashCode();
        foreach (var ev in obj.Suggestions)
        {
            hc.Add(ev);
        }
        hc.Add(obj);

        CompletionOptionComparer.AddHashCode(obj, ref hc);
        hc.Add();
    }
}