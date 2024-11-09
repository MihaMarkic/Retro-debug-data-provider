using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly List<KickAssemblerLexerError> _errors = new();
    public ImmutableArray<KickAssemblerLexerError> Errors => [.._errors];
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
        _errors.Add(new (offendingSymbol, line, charPositionInLine, msg, e));
    }
}

public record KickAssemblerLexerError(int OffendingSymbol, int Line,
    int CharPositionInLine, string Msg, RecognitionException RecognitionException);