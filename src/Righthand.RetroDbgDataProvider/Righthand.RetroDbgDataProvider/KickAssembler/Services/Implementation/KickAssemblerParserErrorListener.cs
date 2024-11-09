﻿using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerParserErrorListener: BaseErrorListener
{
    private readonly List<KickAssemblerParserError> _errors = new();
    public ImmutableArray<KickAssemblerParserError> Errors => [.._errors];
    
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        _errors.Add(new(offendingSymbol, line, charPositionInLine, msg, e));
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}

public record KickAssemblerParserError(IToken OffendingSymbol, int Line, int CharPositionInLine,
    string Msg, RecognitionException RecognitionException);