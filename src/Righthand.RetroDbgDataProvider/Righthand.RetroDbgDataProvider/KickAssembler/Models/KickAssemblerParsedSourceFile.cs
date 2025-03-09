using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public partial class KickAssemblerParsedSourceFile : ParsedSourceFile
{
    public FrozenDictionary<IToken, ReferencedFileInfo> ReferencedFilesMap { get; init; }
    public ImmutableArray<KickAssemblerLexerError> LexerErrors { get; }
    public ImmutableArray<KickAssemblerCodeError> ParserErrors { get; }
    public bool IsImportOnce { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName">Full file name</param>
    /// <param name="relativePath">Relative to either project or library, based on file origin</param>
    /// <param name="allTokens"></param>
    /// <param name="referencedFilesMap"></param>
    /// <param name="inDefines"></param>
    /// <param name="outDefines"></param>
    /// <param name="defaultScope"></param>
    /// <param name="lastModified"></param>
    /// <param name="liveContent"></param>
    /// <param name="isImportOnce"></param>
    /// <param name="lexerErrors"></param>
    /// <param name="parserErrors"></param>
    /// <param name="listenerErrors"></param>
    public KickAssemblerParsedSourceFile(
        string fileName, string relativePath, 
        ImmutableArray<IToken> allTokens,
        FrozenDictionary<IToken, ReferencedFileInfo> referencedFilesMap,
        FrozenSet<string> inDefines,
        FrozenSet<string> outDefines,
        Scope defaultScope,
        DateTimeOffset lastModified,
        string? liveContent,
        bool isImportOnce,
        ImmutableArray<KickAssemblerLexerError> lexerErrors,
        ImmutableArray<KickAssemblerParserError> parserErrors,
        ImmutableArray<KickAssemblerParserSyntaxError> listenerErrors)
        : base(fileName, relativePath, allTokens, referencedFilesMap.Values, inDefines, outDefines, defaultScope,
            lastModified, liveContent)
    {
        IsImportOnce = isImportOnce;
        ReferencedFilesMap = referencedFilesMap;
        LexerErrors = lexerErrors;
        if (!parserErrors.IsEmpty || !listenerErrors.IsEmpty)
        {
            ParserErrors =
            [
                ..parserErrors.CastArray<KickAssemblerCodeError>()
            ];
            ParserErrors = ParserErrors.AddRange(listenerErrors.CastArray<KickAssemblerCodeError>());
        }
        else
        {
            ParserErrors = [];
        }
    }

    /// <inheritdoc />
    public override CompletionOption? GetCompletionOption(TextChangeTrigger trigger, TriggerChar triggerChar, int line, int column,
        string text, int textStart, int textLength, CompletionOptionContext context)
    {
        if (IsLineIgnoredContent(line))
        {
            Debug.WriteLine($"Line {line} is within ignored content");
            return null;
        }

        return GetCompletionOption(Tokens, AllTokensByLineMap, trigger, triggerChar, line, column, text, textStart, textLength, RelativePath, context);
    }

    internal static bool ArePreconditionsValid(TextChangeTrigger trigger, TriggerChar triggerChar, int column, ReadOnlySpan<IToken> lineTokensToCursor)
    {
        switch (trigger)
        {
            case TextChangeTrigger.CharacterTyped:
                switch (triggerChar)
                {
                    case TriggerChar.DoubleQuote:
                        var lastToken = lineTokensToCursor[^1]; 
                        if (lastToken.Type == KickAssemblerLexer.STRING && lastToken.EndColumn() <= column)
                        {
                            return false;
                        }

                        break;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokens">Tokens from default channel</param>
    /// <param name="allTokensByLineMap"></param>
    /// <param name="trigger"></param>
    /// <param name="triggerChar">Character that triggered request, only valid when <see cref="trigger"/> is <see cref="TextChangeTrigger.CharacterTyped"/></param>
    /// <param name="lineNumber">0 based line number in source file</param>
    /// <param name="column">0 based column number within selected line</param>
    /// <param name="text"></param>
    /// <param name="textStart"></param>
    /// <param name="textLength"></param>
    /// <param name="relativePath">Relative path to either project or library, depends on the file origin</param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static CompletionOption? GetCompletionOption(ImmutableArray<IToken> tokens,
        FrozenDictionary<int, ImmutableArray<IToken>> allTokensByLineMap, TextChangeTrigger trigger, TriggerChar triggerChar, int lineNumber,
        int column, string text, int textStart, int textLength, string relativePath, CompletionOptionContext context)
    {
        if (!allTokensByLineMap.TryGetValue(lineNumber, out var allTokensAtLine))
        {
            return null;
        }
        ImmutableArray<IToken> tokensAtLine = [..allTokensAtLine.Where(t => t.Channel == 0)];
        var lineTokens = tokensAtLine.AsSpan();
        var lineToCursor = text.AsSpan().Slice(textStart, column);
        if (lineToCursor.IsEmpty)
        {
            return GenericCompletionOptions.GetOption(lineTokens, text, textStart, textLength, lineNumber, column, context);
        }

        // check preconditions only for implicit request
        if (trigger == TextChangeTrigger.CharacterTyped)
        {
            var columnTokenIndex = tokensAtLine.AsSpan().GetTokenIndexAtColumn(textStart, column);
            if (columnTokenIndex is null)
            {
                return null;
            }

            var lineTokensToCursor = tokensAtLine.AsSpan()[..(columnTokenIndex.Value + 1)];
            if (!ArePreconditionsValid(trigger, triggerChar, column, lineTokensToCursor))
            {
                return null;
            }
        }

        var line = text.AsSpan()[textStart..(textStart + textLength)];
        var result = ArrayCompletionOptions.GetOption(tokens.AsSpan(), text, textStart, textLength, column, relativePath, context)
                     ?? PreprocessorDirectivesCompletionOptionsObsolete.GetOption(lineTokens, text, textStart, textLength, column, context)
                     ?? DirectiveCompletionOptions.GetOption(lineTokens, text, textStart, textLength, column, relativePath, context)
                     ?? FileReferenceCompletionOptions.GetOption(lineTokens, line, trigger, column, relativePath, context)
                     ?? PreprocessorExpressionCompletionOptions.GetOption(lineTokens, text, textStart, textLength, column, context)
                     ?? GenericCompletionOptions.GetOption(lineTokens, text, textStart, textLength, lineNumber, column, context);
        return result;
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
    protected override FrozenDictionary<int, ImmutableArray<IToken>> GetAllTokensPerLine()
    {
        var allTokensByLineMap = AllTokens
            .GroupBy(t => t.Line - 1)
            .ToFrozenDictionary(g => g.Key, g => g.OrderBy(t => t.Column).ToImmutableArray());
        return allTokensByLineMap;
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
        foreach (var t in AllTokens
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
        var tokens = AllTokens;
        var linesCount = tokens.Count(t => t.Type == KickAssemblerLexer.EOL) + 1;
        var lines = GetLexerBasedTokens(linesCount, tokens, CancellationToken.None);
        return lines;
    }

    protected override FrozenDictionary<int, SyntaxErrorLine> GetSyntaxErrors(CancellationToken ct)
    {
        List<SyntaxError> builder = new(LexerErrors.Length + ParserErrors.Length);
        builder.AddRange(LexerErrors.Select(ConvertLexerError));
        builder.AddRange(ParserErrors.Select(ConvertParserError));
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

    private SyntaxError ConvertParserError(KickAssemblerCodeError error)
    {
        return new(error.Message, error.CharPositionInLine, error.Line - 1,
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
        var tokens = AllTokens;
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