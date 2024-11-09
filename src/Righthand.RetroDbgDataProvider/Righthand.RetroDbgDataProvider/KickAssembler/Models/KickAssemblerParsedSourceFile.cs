using System.Collections.Frozen;
using System.ComponentModel;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public class KickAssemblerParsedSourceFile : ParsedSourceFile
{
    public KickAssemblerLexer Lexer { get; init; }
    public CommonTokenStream CommonTokenStream { get; init; }
    public KickAssemblerParser Parser { get; init; }
    public KickAssemblerLexerErrorListener LexerErrorListener { get; init; }
    public KickAssemblerParserErrorListener ParserErrorListener { get; init; }
    public bool IsImportOnce { get; }
    public KickAssemblerParsedSourceFile(
        string fileName,
        ImmutableArray<ReferencedFileInfo> referencedFiles,
        FrozenSet<string> inDefines,
        FrozenSet<string> outDefines,
        DateTimeOffset lastModified,
        string? liveContent,
        KickAssemblerLexer lexer,
        CommonTokenStream commonTokenStream,
        KickAssemblerParser parser,
        KickAssemblerLexerErrorListener lexerErrorListener,
        KickAssemblerParserErrorListener parserErrorListener,
        bool isImportOnce
    ) : base(fileName, referencedFiles, inDefines, outDefines, lastModified, liveContent)
    {
        Lexer = lexer;
        CommonTokenStream = commonTokenStream;
        Parser = parser;
        LexerErrorListener = lexerErrorListener;
        ParserErrorListener = parserErrorListener;
        IsImportOnce = isImportOnce;
    }

    /// <inheritdoc cref="ParsedSourceFile"/>
    internal override ImmutableArray<MultiLineTextRange> GetIgnoredDefineContent(CancellationToken ct)
    {
        var builder = ImmutableArray.CreateBuilder<MultiLineTextRange>();
        IToken? startToken = null;
        IToken? previousToken = null;
        foreach (var t in CommonTokenStream.GetTokens()
                     .Where(t => t.Channel == KickAssemblerLexer.IGNORED))
        {
            if (startToken is null || previousToken is null)
            {
                previousToken = startToken = t;
            }
            else
            {
                // in case of continuous range, extend current one 
                if (previousToken.StopIndex == t.StartIndex - 1)
                {
                    previousToken = t;
                }
                else
                {
                    builder.Add(new MultiLineTextRange(
                        new TextCursor(startToken.Line, startToken.Column),
                        new TextCursor(previousToken.Line, previousToken.Column + previousToken.Text.Length)));
                    startToken = previousToken = t;
                }
            }
        }

        if (startToken is not null && previousToken is not null)
        {
            builder.Add(new MultiLineTextRange(
                new TextCursor(startToken.Line, startToken.Column),
                new TextCursor(previousToken.Line, previousToken.Column + previousToken.Text.Length)));
        }
        return builder.ToImmutable();
    }

    internal override FrozenDictionary<int, SyntaxLine> GetSyntaxLines(CancellationToken ct)
    {
        var tokens = CommonTokenStream.GetTokens();
        var linesCount = tokens.Count(t => t.Type == KickAssemblerLexer.EOL) + 1;
        var lines = IterateTokens(linesCount, tokens, CancellationToken.None);
        return lines;
    }

    internal override FrozenDictionary<int, SyntaxErrorLine> GetSyntaxErrors(CancellationToken ct)
    {
        List<SyntaxError> builder = new(LexerErrorListener.Errors.Length + ParserErrorListener.Errors.Length);
        builder.AddRange(LexerErrorListener.Errors.Select(error => ConvertLexerError(error)));
        builder.AddRange(ParserErrorListener.Errors.Select(error => ConvertParserError(error)));

        return builder.GroupBy(i => i.Line)
            .ToFrozenDictionary(
                g => g.Key,
                g => new SyntaxErrorLine([..g]));
    }

    private SyntaxError ConvertLexerError(KickAssemblerLexerError error)
    {
        return new(error.Msg, null, error.Line - 1, new(error.CharPositionInLine, error.CharPositionInLine + 1),
            SyntaxErrorLexerSource.Default);
    }

    private SyntaxError ConvertParserError(KickAssemblerParserError error)
    {
        return new(error.Msg, error.OffendingSymbol.StartIndex, error.Line - 1,
            new(error.CharPositionInLine, error.CharPositionInLine + 1),
            SyntaxErrorParserSource.Default);
    }

    internal static FrozenDictionary<int, SyntaxLine> IterateTokens(int linesCount, IList<IToken> tokens,
        CancellationToken ct)
    {
        var builder = new Dictionary<int, List<SyntaxItem>>();
    
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            ct.ThrowIfCancellationRequested();
            bool ignore = false;
            if (TokensMap.Map.TryGetValue(token.Type, out var tokenType))
            {
                int lineNumber = token.Line - 1;
                if (!builder.TryGetValue(lineNumber, out List<SyntaxItem>? line))
                {
                    builder[lineNumber] = line = new List<SyntaxItem>();
                }
                int startIndex = token.StartIndex;
                switch (tokenType)
                {
                    case TokenType.Directive:
                        if (i > 0)
                        {
                            var previous = tokens[i - 1];
                            if (previous.Type == KickAssemblerLexer.DOT && previous.Line == token.Line)
                            {
                                startIndex = token.StartIndex - 1;
                            }
                        }
                        break;
                    case TokenType.InstructionExtension:
                        if (i > 1 && tokens[i-1].Type == KickAssemblerLexer.DOT 
                            && TokensMap.Map.TryGetValue(tokens[i-2].Type, out var instructionTokenType) && instructionTokenType == TokenType.Instruction)
                        {
                            startIndex = token.StartIndex - 1;
                        }
                        else
                        {
                            ignore = true;
                        }
                        break;
                }
    
                if (!ignore)
                {
                    line.Add(new SyntaxItem(startIndex, token.StopIndex, tokenType));
                }
            }
        }
        return builder.ToFrozenDictionary(b => b.Key, b => new SyntaxLine([..b.Value]));
    }

    public override SingleLineTextRange? GetTokenRangeAt(int line, int column)
    {
        var tokens = Lexer.GetAllTokens();
        var token = tokens.FirstOrDefault(t => t.Line-1 == line && t.Column <= column && t.Column + t.Text.Length >= column);
        if (token is not null)
        {
            return new SingleLineTextRange(token.Column, token.Column + token.Text.Length);
        }

        return null;
    }
}