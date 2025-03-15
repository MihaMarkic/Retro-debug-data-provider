using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;

/// <summary>
/// Represents a parser error.
/// </summary>
public record KickAssemblerParserError : KickAssemblerCodeError
{
    /// <summary>
    /// Gets a token representing the error.
    /// </summary>
    public IToken OffendingSymbol { get; }
    /// <summary>
    /// Gets recognition exception.
    /// </summary>
    public RecognitionException RecognitionException { get; }
    /// <inheritdoc />
    public override int Line { get; }
    /// <inheritdoc />
    public override int CharPositionInLine { get; }
    /// <inheritdoc />
    public override string Message { get; }
    /// <summary>
    /// Creates an instance of <see cref="KickAssemblerParserError"/>.
    /// </summary>
    /// <param name="offendingSymbol"></param>
    /// <param name="line"></param>
    /// <param name="charPositionInLine"></param>
    /// <param name="message"></param>
    /// <param name="recognitionException"></param>
    public KickAssemblerParserError(IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string message,
        RecognitionException recognitionException)
    {
        OffendingSymbol = offendingSymbol;
        Line = line;
        CharPositionInLine = charPositionInLine;
        Message = message;
        RecognitionException = recognitionException;
    }
}