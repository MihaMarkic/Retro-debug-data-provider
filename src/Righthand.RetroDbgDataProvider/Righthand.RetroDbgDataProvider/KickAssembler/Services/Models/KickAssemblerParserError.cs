using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;

public record KickAssemblerParserError : KickAssemblerCodeError
{
    public IToken OffendingSymbol { get; }
    public RecognitionException RecognitionException { get; }
    public override int Line { get; }
    public override int CharPositionInLine { get; }
    public override string Message { get; }

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