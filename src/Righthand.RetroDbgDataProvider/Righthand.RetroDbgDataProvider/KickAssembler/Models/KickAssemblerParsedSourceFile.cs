using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public partial class KickAssemblerParsedSourceFile : ParsedSourceFile
{
    public KickAssemblerLexer Lexer { get; init; }
    public CommonTokenStream CommonTokenStream { get; init; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public KickAssemblerParser Parser { get; init; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
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
        FrozenSet<SegmentDefinitionInfo> segmentDefinitions,
        DateTimeOffset lastModified,
        string? liveContent,
        KickAssemblerLexer lexer,
        CommonTokenStream commonTokenStream,
        KickAssemblerParser parser,
        KickAssemblerParserListener parserListener,
        KickAssemblerLexerErrorListener lexerErrorListener,
        KickAssemblerParserErrorListener parserErrorListener,
        bool isImportOnce
    ) : base(fileName, referencedFilesMap.Values, inDefines, outDefines, segmentDefinitions, lastModified, liveContent)
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

    /// <inheritdoc />
    public override CompletionOption? GetCompletionOption(TextChangeTrigger trigger, int line, int column,
        string text, int textStart, int textLength) =>
        GetCompletionOption(AllTokensByLineMap, trigger, line, column, text, textStart, textLength);

    internal static CompletionOption? GetCompletionOption(
        FrozenDictionary<int, ImmutableArray<IToken>> allTokensByLineMap, TextChangeTrigger trigger, int line,
        int column, string text, int textStart, int textLength)
    {
        if (!allTokensByLineMap.TryGetValue(line, out var tokensAtLine))
        {
            return null;
        }

        var textSpan = text.AsSpan()[textStart..(textStart + textLength)];
        CompletionOption? result =
            (
                FileReferenceCompletionOptions.GetOption(tokensAtLine.AsSpan(), textSpan, trigger, column) ??
                PreprocessorDirectivesCompletionOptions.GetOption(textSpan, trigger, column)) ??
                QuotedWithinArrayCompletionOptions.GetOption(tokensAtLine.AsSpan(), text, textStart, textLength, trigger,
                    column,
                    ValuesCount.Multiple) ??
                QuotedCompletionOptions.GetOption(tokensAtLine.AsSpan(), text, textStart, textLength, trigger,
                    column
            );

        return result;
    }


    /// <inheritdoc />
    public override ImmutableArray<string> GetPreprocessorDirectiveSuggestions(string root)
    {
        return GetMatches(root.AsSpan(), 1, KickAssemblerLexer.PreprocessorDirectives.AsSpan());
    }
    /// <summary>
    /// Matches all those items from list that have same root.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="startIndex">Start index in list's item</param>
    /// <param name="list"></param>
    /// <returns></returns>
    internal static ImmutableArray<string> GetMatches(ReadOnlySpan<char> root, int startIndex, ReadOnlySpan<string> list)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        bool hasFirstMatch = false;
        foreach (var i in list)
        {
            bool isMatch = i.Length >= root.Length + startIndex;
            if (isMatch)
            {
                var substring = i.AsSpan()[startIndex .. (startIndex + root.Length)];
                isMatch = substring.SequenceEqual(root);
            }

            if (hasFirstMatch)
            {
                if (isMatch)
                {
                    builder.Add(i);
                }
                else
                {
                    break;
                }
            }
            else if (isMatch)
            {
                builder.Add(i);
                hasFirstMatch = true;
            }
        }
        return builder.ToImmutable();
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

    // ReSharper disable once UnusedParameter.Local
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
                    case TokenType.PreprocessorDirective:
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
    
    public enum ValuesCount
    {
        Single,
        Multiple,
    }
}