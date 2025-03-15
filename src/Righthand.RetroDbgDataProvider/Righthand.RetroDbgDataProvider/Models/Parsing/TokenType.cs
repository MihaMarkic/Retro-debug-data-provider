namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents type of a token.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// String.
    /// </summary>
    String,
    /// <summary>
    /// Assembler mnemonic.
    /// </summary>
    Instruction,
    /// <summary>
    /// An operator.
    /// </summary>
    Operator,
    /// <summary>
    /// A number.
    /// </summary>
    Number,
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown,
    /// <summary>
    /// A comment.
    /// </summary>
    Comment,
    /// <summary>
    /// A directive.
    /// </summary>
    Directive,
    /// <summary>
    /// A preprocessor directive starting with a hash.
    /// </summary>
    PreprocessorDirective,
    /// <summary>
    /// A color.
    /// </summary>
    Color,
    /// <summary>
    /// An instruction extension.
    /// </summary>
    InstructionExtension,
    /// <summary>
    /// A bracket.
    /// </summary>
    Bracket,
    /// <summary>
    /// A seprator.
    /// </summary>
    Separator,
    /// <summary>
    /// References a file, typically from #import or #import if directive
    /// </summary>
    FileReference,
    /// <summary>
    /// Anything else.
    /// </summary>
    Other,
}