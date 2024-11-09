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
    Color,
    InstructionExtension,
    Bracket,
    Separator
}