using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

internal class KickAssemblerLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly List<KickAssemblerLexerError> _errors = new();
    internal ImmutableArray<KickAssemblerLexerError> Errors => [.._errors];
    /// <summary>
    /// Adds a <see cref="SyntaxError"/> to the list.
    /// </summary>
    /// <param name="output"></param>
    /// <param name="recognizer"></param>
    /// <param name="offendingSymbol"></param>
    /// <param name="line"></param>
    /// <param name="charPositionInLine"></param>
    /// <param name="msg"></param>
    /// <param name="e"></param>
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
        _errors.Add(new (offendingSymbol, line, charPositionInLine, msg, e));
    }
}
/// <summary>
/// Represents a lexer error.
/// </summary>
/// <param name="OffendingSymbol"></param>
/// <param name="Line"></param>
/// <param name="CharPositionInLine"></param>
/// <param name="Msg"></param>
/// <param name="RecognitionException"></param>
public record KickAssemblerLexerError(int OffendingSymbol, int Line,
    int CharPositionInLine, string Msg, RecognitionException RecognitionException);