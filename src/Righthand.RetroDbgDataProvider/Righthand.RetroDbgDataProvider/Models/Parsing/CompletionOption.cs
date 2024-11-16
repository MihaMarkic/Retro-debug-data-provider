namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum CompletionOptionType
{
    FileReference,
}

public enum TextChangeTrigger
{
    CharacterTyped,
    CompletionRequested,
}

public record struct  CompletionOption(CompletionOptionType Type, string Root, bool EndsWithDoubleQuote);