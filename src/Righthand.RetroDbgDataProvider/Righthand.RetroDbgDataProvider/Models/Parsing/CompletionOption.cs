using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Source of the text change.
/// </summary>
public enum TextChangeTrigger
{
    /// <summary>
    /// A character was typed. Usually one from <see cref="TextChangeTrigger"/>.
    /// </summary>
    CharacterTyped,
    /// <summary>
    /// Completion was requested through a keyboard shortcut or otherwise.
    /// </summary>
    CompletionRequested,
}
/// <summary>
/// Origin of the suggesion.
/// </summary>
public enum SuggestionOrigin
{
    /// <summary>
    /// A preprocessor directive.
    /// </summary>
    PreprocessorDirective,
    /// <summary>
    /// A file reference.
    /// </summary>
    File,
    /// <summary>
    /// A directory.
    /// </summary>
    Directory,
    /// <summary>
    /// A property name.
    /// </summary>
    PropertyName,
    /// <summary>
    /// A property value.
    /// </summary>
    PropertyValue,
    /// <summary>
    /// A directive option.
    /// </summary>
    DirectiveOption,
    /// <summary>
    /// A label.
    /// </summary>
    Label,
    /// <summary>
    /// A variable.
    /// </summary>
    Variable,
    /// <summary>
    /// A constant.
    /// </summary>
    Constant,
    /// <summary>
    /// An enum value.
    /// </summary>
    EnumValue,
    /// <summary>
    /// A macro.
    /// </summary>
    Macro,
    /// <summary>
    /// A function.
    /// </summary>
    Function,
    /// <summary>
    /// A color.
    /// </summary>
    Color,
    /// <summary>
    /// A built-in math function.
    /// </summary>
    Math,
    /// <summary>
    /// An assembler mnemonic.
    /// </summary>
    Mnemonic
}
/// <summary>
/// Base class for suggestion.
/// </summary>
/// <param name="Origin"></param>
/// <param name="Text"></param>
/// <param name="Priority"></param>
public abstract record Suggestion(SuggestionOrigin Origin, string Text, int Priority)
{
    /// <summary>
    /// Gets whether suggestion is considered a default one.
    /// </summary>
    public bool IsDefault { get; init; }
}
/// <summary>
/// Generic suggestion.
/// </summary>
/// <param name="Origin"></param>
/// <param name="Text"></param>
/// <param name="Priority"></param>
public record StandardSuggestion(SuggestionOrigin Origin, string Text, int Priority = 0) : Suggestion(Origin, Text, Priority);
/// <summary>
/// Base class for file system reference suggestions.
/// </summary>
/// <param name="Origin"></param>
/// <param name="Text"></param>
/// <param name="FileOrigin"></param>
/// <param name="OriginPath"></param>
/// <param name="Priority"></param>
public abstract record FileSystemSuggestion(SuggestionOrigin Origin, string Text, ProjectFileOrigin FileOrigin, string OriginPath, int Priority = 0) : Suggestion(Origin, Text, Priority);
/// <summary>
/// File reference suggestion.
/// </summary>
/// <param name="Text"></param>
/// <param name="FileOrigin"></param>
/// <param name="OriginPath"></param>
/// <param name="Priority"></param>
public record FileSuggestion(string Text, ProjectFileOrigin FileOrigin, string OriginPath, int Priority = 0) : FileSystemSuggestion(SuggestionOrigin.File, Text, FileOrigin, OriginPath, Priority);
/// <summary>
/// Directory suggeston.
/// </summary>
/// <param name="Text"></param>
/// <param name="FileOrigin"></param>
/// <param name="OriginPath"></param>
/// <param name="Priority"></param>
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
    /// Gets default instance representing an empty value.
    /// </summary>
    public static CompletionOption Empty => new CompletionOption("", 0, "", "", []);
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
    /// <summary>
    /// Creates an instance of <see cref="CompletionOption"/>.
    /// </summary>
    /// <param name="rootText"></param>
    /// <param name="replacementLength"></param>
    /// <param name="prependText"></param>
    /// <param name="appendText"></param>
    /// <param name="suggestions"></param>
    public CompletionOption(string rootText, int replacementLength, string prependText, string appendText, FrozenSet<Suggestion> suggestions)
    {
        RootText = rootText;
        ReplacementLength = replacementLength;
        AppendText = appendText;
        PrependText = prependText;
        Suggestions = suggestions;
    }

    /// <summary>
    /// Deconstructs.
    /// </summary>
    /// <param name="rootText"></param>
    /// <param name="replacementLength"></param>
    /// <param name="prependText"></param>
    /// <param name="appendText"></param>
    public void Deconstruct(out string rootText, out int replacementLength, out string prependText, out string appendText)
    {
        rootText = RootText;
        replacementLength = ReplacementLength;
        prependText = PrependText;
        appendText = AppendText;
    }
    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is CompletionOption other)
        {
            return other.RootText.Equals(RootText) && other.ReplacementLength.Equals(ReplacementLength)
                && other.AppendText.Equals(AppendText) && other.Suggestions.SetEquals(Suggestions);
        }
        return false;
    }
    /// <inheritdoc />
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