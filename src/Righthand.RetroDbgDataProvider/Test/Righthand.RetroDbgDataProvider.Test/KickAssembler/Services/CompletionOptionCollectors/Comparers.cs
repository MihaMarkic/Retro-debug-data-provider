using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class CompletionOptionComparer: IEqualityComparer<CompletionOption>
{
    public static CompletionOptionComparer Default { get; } = new CompletionOptionComparer();
    public bool Equals(CompletionOption x, CompletionOption y)
    {
        return x.Type == y.Type && x.Root == y.Root && x.EndsWithDoubleQuote == y.EndsWithDoubleQuote && x.ReplacementLength == y.ReplacementLength &&
               x.ExcludedValues.SetEquals(y.ExcludedValues) && x.ValueType == y.ValueType;
    }

    public int GetHashCode(CompletionOption obj)
    {
        long excludedValuesHash = 0;
        foreach (var ev in obj.ExcludedValues)
        {
            excludedValuesHash = HashCode.Combine(excludedValuesHash, ev.GetHashCode());
        }

        return HashCode.Combine((int)obj.Type, obj.Root, obj.EndsWithDoubleQuote, obj.ReplacementLength, excludedValuesHash, obj.ValueType);
    }
}