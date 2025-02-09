using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum TextChangeTrigger
{
    CharacterTyped,
    CompletionRequested,
}

public enum SuggestionOrigin
{
    PreprocessorDirective,
    File,
    Directory,
    PropertyName,
    PropertyValue,
    DirectiveOption,
    Label,
    Variable,
    Constant,
}

public abstract record Suggestion(SuggestionOrigin Origin, string Text, int Priority)
{
    public bool IsDefault { get; init; }
}
public record StandardSuggestion(SuggestionOrigin Origin, string Text, int Priority = 0) : Suggestion(Origin, Text, Priority);
public abstract record FileSystemSuggestion(SuggestionOrigin Origin, string Text, ProjectFileOrigin FileOrigin, string OriginPath, int Priority = 0) : Suggestion(Origin, Text, Priority);
public record FileSuggestion(string Text, ProjectFileOrigin FileOrigin, string OriginPath, int Priority = 0) : FileSystemSuggestion(SuggestionOrigin.File, Text, FileOrigin, OriginPath, Priority);
public record DirectorySuggestion(string Text, ProjectFileOrigin FileOrigin, string OriginPath, int Priority = 0) : FileSystemSuggestion(SuggestionOrigin.Directory, Text, FileOrigin, OriginPath, Priority);

/// <summary>
/// Represents a completion option containing valid suggestions.
/// </summary>
/// <remarks>
/// Fully equals by value.
/// </remarks>
public readonly struct CompletionOption
{
    /// <summary>
    /// Text to the left of the caret to be replaced.
    /// </summary>
    public string RootText { get; }
    /// <summary>
    /// Length of replaced text. This value is always present regardless of suggestions.
    /// </summary>
    public int ReplacementLength { get; }
    /// <summary>
    /// Appended text.
    /// </summary>
    public string AppendText { get; }
    /// <summary>
    /// Prepended text. Currently, for " only.
    /// </summary>
    public string PrependText { get; }
    /// <summary>
    /// A set of suggestions.
    /// </summary>
    public FrozenSet<Suggestion> Suggestions { get; }
    public CompletionOption(string rootText, int replacementLength, string prependText, string appendText, FrozenSet<Suggestion> suggestions)
    {
        RootText = rootText;
        ReplacementLength = replacementLength;
        AppendText = appendText;
        PrependText = prependText;
        Suggestions = suggestions;
    }

    public void Deconstruct(out string rootText, out int replacementLength, out string prependText, out string appendText)
    {
        rootText = RootText;
        replacementLength = ReplacementLength;
        prependText = PrependText;
        appendText = AppendText;
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