using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

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

public readonly struct CompletionOption
{
    public string RootText { get; }
    public int ReplacementLength { get; }
    public string AppendText { get; }
    public FrozenSet<Suggestion> Suggestions { get; }
    public CompletionOption(string rootText, int replacementLength, string appendText, FrozenSet<Suggestion> suggestions)
    {
        RootText = rootText;
        ReplacementLength = replacementLength;
        AppendText = appendText;
        Suggestions = suggestions;
    }
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is CompletionOption other)
        {
            return other.RootText.Equals(RootText) && other.ReplacementLength.Equals(ReplacementLength)
                && other.AppendText.Equals(AppendText) && other.Suggestions.SetEquals(Suggestions);
        }
        return false;
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (var ev in Suggestions)
        {
            hc.Add(ev);
        }
        hc.Add(RootText);
        hc.Add(ReplacementLength);
        hc.Add(AppendText);
        return hc.ToHashCode();
    }
}