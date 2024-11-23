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
    public KickAssemblerParserListener ParserListener { get; init; }
    public KickAssemblerLexerErrorListener LexerErrorListener { get; init; }
    public KickAssemblerParserErrorListener ParserErrorListener { get; init; }
    public FrozenDictionary<IToken, ReferencedFileInfo> ReferencedFilesMap { get; init; }
    public bool IsImportOnce { get; }

    public KickAssemblerParsedSourceFile(
        string fileName,
        FrozenDictionary<IToken, ReferencedFileInfo> referencedFilesMap,
        FrozenSet<string> inDefines,
        FrozenSet<string> outDefines,
        DateTimeOffset lastModified,
        string? liveContent,
        KickAssemblerLexer lexer,
        CommonTokenStream commonTokenStream,
        KickAssemblerParser parser,
        KickAssemblerParserListener parserListener,
        KickAssemblerLexerErrorListener lexerErrorListener,
        KickAssemblerParserErrorListener parserErrorListener,
        bool isImportOnce
    ) : base(fileName, referencedFilesMap.Values, inDefines, outDefines, lastModified, liveContent)
    {
        Lexer = lexer;
        CommonTokenStream = commonTokenStream;
        Parser = parser;
        ParserListener = parserListener;
        LexerErrorListener = lexerErrorListener;
        ParserErrorListener = parserErrorListener;
        IsImportOnce = isImportOnce;
        ReferencedFilesMap = referencedFilesMap;
    }

    public override CompletionOption? GetCompletionOption(TextChangeTrigger trigger, int line, int column) =>
        GetCompletionOption(AllTokensByLineMap, trigger, line, column);

    internal static CompletionOption? GetCompletionOption(
        FrozenDictionary<int, ImmutableArray<IToken>> allTokensByLineMap, TextChangeTrigger trigger, int line,
        int column)
    {
        CompletionOption? result = null;
        var tokenIndex = GetTokenIndexAtLocation(allTokensByLineMap, line, column - 1);
        if (tokenIndex is not null)
        {
            result = IsFileReferenceCompletionOption(allTokensByLineMap[line].AsSpan(), trigger, tokenIndex.Value, column);
        }

        return result;
    }

    internal static CompletionOption? IsFileReferenceCompletionOption(
        ReadOnlySpan<IToken> tokens, TextChangeTrigger trigger, int tokenIndex, int column)
    {
        int doubleQuoteTokenIndex;
        int firstRootTextTokenIndex;
        var token = tokens[tokenIndex];
        switch (trigger)
        {
            case TextChangeTrigger.CharacterTyped:
                switch (token.Type)
                {
                    case KickAssemblerLexer.DOUBLE_QUOTE:
                        firstRootTextTokenIndex = tokenIndex + 1;
                        break;
                    case KickAssemblerLexer.STRING:
                        firstRootTextTokenIndex = tokenIndex;
                        break;
                    default:
                        return null;
                }
                doubleQuoteTokenIndex = tokenIndex;
                break;
            case TextChangeTrigger.CompletionRequested:
                if (token.Type is not (KickAssemblerLexer.STRING or KickAssemblerLexer.UNQUOTED_STRING
                    or KickAssemblerLexer.DOUBLE_QUOTE))
                {
                    return null;
                }

                if (token.Type is KickAssemblerLexer.DOUBLE_QUOTE or KickAssemblerLexer.STRING)
                {
                    firstRootTextTokenIndex = token.Type switch
                    {
                        KickAssemblerLexer.DOUBLE_QUOTE => tokenIndex + 1,
                        KickAssemblerLexer.STRING => tokenIndex,
                        _ => throw new NotImplementedException(),
                    };
                    doubleQuoteTokenIndex = tokenIndex;
                }
                else
                {
                    firstRootTextTokenIndex = tokenIndex;
                    // optimistically assume previous token is "
                    doubleQuoteTokenIndex = tokenIndex - 1;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(trigger));
        }

        var previousTokenIndex = GetPreviousDefaultChannelTokenIndex(tokens, doubleQuoteTokenIndex);
        if (previousTokenIndex is not null)
        {
            var previousTokenType = tokens[previousTokenIndex.Value].Type;
            bool isMatch =
                previousTokenType is KickAssemblerLexer.HASHIMPORT or KickAssemblerLexer.HASHIMPORTIF;
            if (!isMatch)
            {
                isMatch = IsImportIfCommand(tokens, previousTokenIndex.Value);
            }

            if (isMatch)
            {
                string root = token.Type switch
                {
                    KickAssemblerLexer.DOUBLE_QUOTE => string.Empty,
                    KickAssemblerLexer.STRING => token.Text.Substring(1, Math.Max(0, column - token.Column - 1)),
                    KickAssemblerLexer.UNQUOTED_STRING => token.Text[..(column - token.Column)],
                    _ => string.Empty,
                };
                var (replaceableLength, endsWithDoubleQuote) =
                    GetReplaceableTextLength(tokens[firstRootTextTokenIndex..]);
                return new CompletionOption(CompletionOptionType.FileReference, root,
                    endsWithDoubleQuote, replaceableLength);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets total length to replace and whether text ends with a double quote when intellisense choice is used.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    /// <remarks>First token might be DOUBLE_QUOTE or STRING</remarks>
    internal static (int Length, bool EndsWithDoubleQuote) GetReplaceableTextLength(ReadOnlySpan<IToken> tokens)
    {
        var firstToken = tokens[0];
        if (firstToken.Type == KickAssemblerLexer.STRING)
        {
            return (firstToken.Length() - 2, true);
        }
        else if (firstToken.Type is KickAssemblerLexer.EOL or KickAssemblerLexer.Eof)
        {
            return (0, false);
        }
        int start = firstToken.Column;
        int current = 1;
        while (current < tokens.Length)
        {
            var token = tokens[current];
            switch (token.Type)
            {
                case KickAssemblerLexer.DOUBLE_QUOTE:
                    return (token.Column + token.Length() - start, true);
                case KickAssemblerLexer.EOL:
                    return (token.Column - start - 1, false);
                case KickAssemblerLexer.Eof:
                    return (token.Column - start, false);
                default:
                    current++;
                    break;
            }
        }

        return (firstToken.Length(), false);
    }

/// <summary>
    /// Returns previous token to <param name="tokenIndex"> on default channel.</param>
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="tokenIndex"></param>
    /// <param name="channel">Channel to take token from</param>
    /// <returns></returns>
    internal static int? GetPreviousDefaultChannelTokenIndex(ReadOnlySpan<IToken> tokens, int tokenIndex, int channel = 0)
    {
        if (tokenIndex > 1 && tokenIndex <= tokens.Length)
        {
            int targetIndex = tokenIndex - 1;
            while (targetIndex >= 0)
            {
                if (tokens[targetIndex].Channel == channel)
                {
                    return targetIndex;
                }
                targetIndex--;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns whether tokens are suitable for #importif command suggestion.
    /// </summary>
    /// <param name="tokens">Tokens from all channels</param>
    /// <param name="tokenIndex">First token to check</param>
    /// <returns></returns>
    internal static bool IsImportIfCommand(ReadOnlySpan<IToken> tokens, int tokenIndex)
    {
        if (tokenIndex > 0)
        {
            int currentIndex = tokenIndex;
            while (currentIndex >= 0 && tokens[currentIndex].Type != KickAssemblerLexer.EOL)
            {
                var token = tokens[currentIndex];
                if (token.Channel == 0)
                {
                    switch (token.Type)
                    {
                        case KickAssemblerLexer.DOUBLE_QUOTE:
                        case KickAssemblerLexer.STRING:
                            return false;
                        case KickAssemblerLexer.HASHIMPORTIF:
                            return true;
                    }
                }

                currentIndex--;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates an array of <see cref="IToken"/> and map by 0 based line index with tokens from all channels.
    /// </summary>
    protected override (ImmutableArray<IToken> AllTokens, FrozenDictionary<int, ImmutableArray<IToken>>
        AllTokensByLineMap) GetAllTokens()
    {
        Lexer.Reset();
        var stream = new BufferedTokenStream(Lexer);
        stream.Fill();
        ImmutableArray<IToken> allTokens = [..stream.GetTokens()];
        var allTokensByLineMap = allTokens
            .GroupBy(t => t.Line - 1)
            .ToFrozenDictionary(g => g.Key, g => g.OrderBy(t => t.Column).ToImmutableArray());
        return (allTokens, allTokensByLineMap);
    }

    // /// <summary>
    // /// Returns token at location defined by <param name="offset"/> or null.
    // /// </summary>
    // /// <param name="tokens">A collection of <see cref="IToken"/></param>
    // /// <param name="offset">Offset from the start</param>
    // /// <returns>A <see cref="IToken"/> at that location or null otherwise.</returns>
    // internal static int? GetTokenIndexAtLocation(ImmutableArray<IToken> tokens, int offset)
    // {
    //     if (offset < 0)
    //     {
    //         return null;
    //     }
    //
    //     for (int i = 0; i < tokens.Length; i++)
    //     {
    //         var token = tokens[i];
    //         // EOF has -1, thus check start index+1
    //         int stopIndex = token.Type != KickAssemblerLexer.Eof ? token.StopIndex : token.StartIndex + 1;
    //         if (token.StartIndex <= offset && stopIndex >= offset)
    //         {
    //             return i;
    //         }
    //     }
    //
    //     return null;
    // }
    /// <summary>
    /// Returns token at location defined by <param name="line"/> and <param name="column"/>.
    /// </summary>
    /// <param name="tokensMap">0 based line map of tokens</param>
    /// <param name="line">0 based line index</param>
    /// <param name="column">0 based column index</param>
    /// <returns></returns>
    internal static int? GetTokenIndexAtLocation(FrozenDictionary<int, ImmutableArray<IToken>> tokensMap, int line, int column)
    {
        if (line < 0 || column < 0 || !tokensMap.TryGetValue(line, out ImmutableArray<IToken> tokensAtLine))
        {
            return null;
        }

        for (int i = 0; i < tokensAtLine.Length; i++)
        {
            var token = tokensAtLine[i];
            if (token.Column > column)
            {
                break;
            }
            // EOF has -1, thus check start index+1
            int stopColumn = token.Type != KickAssemblerLexer.Eof ? token.Column + token.Length() : token.Column + 1;
            if (token.Column <= column && stopColumn > column)
            {
                return i;
            }
        }

        return null;
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

    protected override async Task<FrozenDictionary<int, SyntaxLine>> GetSyntaxLinesAsync(CancellationToken ct)
    {
        var lexerBasedSyntaxLinesTask = Task.Run(() => GetLexerBasedSyntaxLines(ct), ct).ConfigureAwait(false);
        var lexerBasedSyntaxLines = await lexerBasedSyntaxLinesTask;
        var updatedLines = UpdateLexerAnalysisWithParserAnalysis(lexerBasedSyntaxLines, ReferencedFilesMap);
        return updatedLines.GroupBy(l => l.LineNumber)
            .ToFrozenDictionary(
                g => g.Key,
                g => new SyntaxLine([..g.Select(i => i.Item)]));
    }

    internal static IEnumerable<LexerBasedSyntaxResult> UpdateLexerAnalysisWithParserAnalysis(
        ImmutableArray<LexerBasedSyntaxResult> source, FrozenDictionary<IToken, ReferencedFileInfo> fileReferences)
    {
        // updates lexer based results with parser based analysis
        var updatedLines = source.Select(l =>
        {
            // if token matches file reference, then create FileReferenceSyntaxItem substitution for SyntaxItem 
            if (fileReferences.TryGetValue(l.Token, out var replacementItem))
            {
                return l with
                {
                    Item = new FileReferenceSyntaxItem(l.Item.Start, l.Item.End, replacementItem)
                    {
                        LeftMargin = 1,
                        RightMargin = 1,
                    }
                };
            }

            //otherwise merely return existing item
            return l;
        });
        return updatedLines;
    }

    internal record LexerBasedSyntaxResult(int LineNumber, IToken Token, SyntaxItem Item);

    private ImmutableArray<LexerBasedSyntaxResult> GetLexerBasedSyntaxLines(CancellationToken ct)
    {
        var tokens = CommonTokenStream.GetTokens();
        var linesCount = tokens.Count(t => t.Type == KickAssemblerLexer.EOL) + 1;
        var lines = GetLexerBasedTokens(linesCount, tokens, CancellationToken.None);
        return lines;
    }

    protected override FrozenDictionary<int, SyntaxErrorLine> GetSyntaxErrors(CancellationToken ct)
    {
        List<SyntaxError> builder = new(LexerErrorListener.Errors.Length + ParserErrorListener.Errors.Length);
        builder.AddRange(LexerErrorListener.Errors.Select(ConvertLexerError));
        builder.AddRange(ParserErrorListener.Errors.Select(ConvertParserError));
        builder.AddRange(CreateMissingReferencedFilesErrors());
        return builder.GroupBy(i => i.Line)
            .ToFrozenDictionary(
                g => g.Key,
                g => new SyntaxErrorLine([..g]));
    }

    IEnumerable<SyntaxError> CreateMissingReferencedFilesErrors()
    {
        var errors = ReferencedFilesMap
                .Where(m => m.Value.FullFilePath is null)
                .Select(m =>
                    new SyntaxError($"Missing referenced file {m.Value.RelativeFilePath}", m.Key.StartIndex,
                        m.Key.Line - 1,
                        new SingleLineTextRange(m.Key.Column, m.Key.EndColumn()), SyntaxErrorParserSource.Default))
            ;
        return errors;
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

    internal static ImmutableArray<LexerBasedSyntaxResult> GetLexerBasedTokens(int linesCount, IList<IToken> tokens,
        CancellationToken ct)
    {
        var builder = ImmutableArray.CreateBuilder<LexerBasedSyntaxResult>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            ct.ThrowIfCancellationRequested();
            bool ignore = false;
            if (TokensMap.Map.TryGetValue(token.Type, out var tokenType))
            {
                int lineNumber = token.Line - 1;
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
                        if (i > 1 && tokens[i - 1].Type == KickAssemblerLexer.DOT
                                  && TokensMap.Map.TryGetValue(tokens[i - 2].Type, out var instructionTokenType) &&
                                  instructionTokenType == TokenType.Instruction)
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
                    builder.Add(new LexerBasedSyntaxResult(lineNumber, token,
                        new SyntaxItem(startIndex, token.StopIndex, tokenType)));
                }
            }
        }

        return builder.ToImmutable();
    }

    public override SingleLineTextRange? GetTokenRangeAt(int line, int column)
    {
        var tokens = Lexer.GetAllTokens();
        var token = tokens.FirstOrDefault(t =>
            t.Line - 1 == line && t.Column <= column && t.Column + t.Text.Length >= column);
        if (token is not null)
        {
            return new SingleLineTextRange(token.Column, token.Column + token.Text.Length);
        }

        return null;
    }
}