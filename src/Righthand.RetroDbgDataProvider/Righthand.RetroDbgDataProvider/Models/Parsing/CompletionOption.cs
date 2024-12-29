using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Comparers;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum TextChangeTrigger
{
    CharacterTyped,
    CompletionRequested,
}

public enum SuggestionOrigin
{
    File,
    PropertyName,
    PropertyValue,
}

public record Suggestion(SuggestionOrigin Origin, string Text);

public abstract record CompletionOption(string RootText, int ReplacementLength)
{
    protected void AddHashCode(ref HashCode hc)
    {
        hc.Add(RootText);
        hc.Add(ReplacementLength);
    }
}

public enum FileType
{
    Source,
    Text,
    Program,
    Binary,
    Sid
}

public record FileCompletionOption(string RootText, int ReplacementLength, FileType FileType, bool AppendDoubleQuote, FrozenSet<string> ExcludedValues)
    : CompletionOption(RootText, ReplacementLength);

public record SegmentCompletionOption(string RootText, int ReplacementLength, FrozenSet<string> ExcludedValues) : CompletionOption(RootText, ReplacementLength);

public record KeywordCompletionOption(string RootText, int ReplacementLength) : CompletionOption(RootText, ReplacementLength);

public record ArrayPropertyNameCompletionOption(string RootText, int ReplacementLength, FrozenSet<Suggestion> Suggestions) : CompletionOption(RootText, ReplacementLength)
{
    public override int GetHashCode()
    {
        var hc = new HashCode();
        base.AddHashCode(ref hc);
        Suggestions.AddHashCode(ref hc);
        return hc.ToHashCode();
    }
}

public record ArrayPropertyValueCompletionOption(string RootText, int ReplacementLength, bool AppendDoubleQuote, ArrayProperty? Property, FrozenSet<Suggestion>? Suggestions) :CompletionOption(RootText, ReplacementLength);