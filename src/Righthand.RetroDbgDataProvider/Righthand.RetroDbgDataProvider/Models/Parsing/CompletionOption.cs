using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum CompletionOptionType
{
    FileReference,
    PreprocessorDirective,
    ProgramFile,
    TextFile,
    BinaryFile,
    SidFile,
    Segments,
    ArrayPropertyName,
    ArrayPropertyValue,
}

public enum TextChangeTrigger
{
    CharacterTyped,
    CompletionRequested,
}

/// <summary>
/// 
/// </summary>
/// <param name="Type"></param>
/// <param name="Root"></param>
/// <param name="EndsWithDoubleQuote"></param>
/// <param name="ReplacementLength"></param>
/// <param name="ExcludedValues">Relative paths of excluded files</param>
/// <param name="DirectiveType">Additional parameter, such as '.file' from '.file c64 [....]'</param>
public record struct CompletionOption(CompletionOptionType Type, string Root, bool EndsWithDoubleQuote, int ReplacementLength,
    FrozenSet<string> ExcludedValues, string? DirectiveType = null, string? ValueType = null);