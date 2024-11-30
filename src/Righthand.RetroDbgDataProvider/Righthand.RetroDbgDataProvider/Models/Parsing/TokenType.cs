namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public enum TokenType
{
    String,
    Instruction,
    Operator,
    Number,
    Unknown,
    Comment,
    Directive,
    PreprocessorDirective,
    Color,
    InstructionExtension,
    Bracket,
    Separator,
    /// <summary>
    /// References a file, typically from #import or #import if directive
    /// </summary>
    FileReference,
}