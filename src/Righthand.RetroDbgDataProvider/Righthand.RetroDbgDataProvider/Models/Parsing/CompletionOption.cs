namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum CompletionOptionType
{
    FileReference,
    PreprocessorDirective,
    ProgramFile,
    SidFile,
    Segments,
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
/// <param name="ExcludedFiles">Relative paths of excluded files</param>
public record struct CompletionOption(CompletionOptionType Type, string Root, bool EndsWithDoubleQuote, int ReplacementLength,
    ImmutableArray<string> ExcludedFiles);