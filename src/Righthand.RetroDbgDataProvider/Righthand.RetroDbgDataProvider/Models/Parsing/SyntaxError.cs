using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents any syntax error including ones from compiler.
/// </summary>
/// <param name="Text">Error message describing the problem.</param>
/// <param name="Offset">Character offset from start.</param>
/// <param name="Line">0 based line.</param>
/// <param name="Range">Error span.</param>
/// <param name="Source">Source of the error.</param>
public record SyntaxError(string Text, int? Offset, int Line, SingleLineTextRange Range, SyntaxErrorSource Source);

/// <summary>
/// Base record for error sources.
/// </summary>
public abstract record SyntaxErrorSource
{}

/// <summary>
/// Error source is Lexer.
/// </summary>
public record SyntaxErrorLexerSource : SyntaxErrorSource
{
    /// <summary>
    /// Gets a default static instance.
    /// </summary>
    public static SyntaxErrorLexerSource Default { get; } = new();
}
/// <summary>
/// Error source is Parser.
/// </summary>
public record SyntaxErrorParserSource: SyntaxErrorSource
{
    /// <summary>
    /// Gets a default static instance.
    /// </summary>
    public static SyntaxErrorParserSource Default { get; } = new();
}
/// <summary>
/// Error source is design time project file.
/// </summary>
/// <param name="IsInMemory"></param>
public record SyntaxErrorFileSource(bool IsInMemory): SyntaxErrorSource;
/// <summary>
/// Error source is compiler output.
/// </summary>
public record SyntaxErrorCompiledFileSource : SyntaxErrorSource
{
    /// <summary>
    /// Gets a default static instance.
    /// </summary>
    public static SyntaxErrorCompiledFileSource Default { get; } = new();
}