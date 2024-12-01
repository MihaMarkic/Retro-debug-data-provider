using System.Collections.Frozen;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Models;

public partial class KickAssemblerParsedSourceFile : ParsedSourceFile
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

    /// <inheritdoc />
    public override CompletionOption? GetCompletionOption(TextChangeTrigger trigger, int line, int column,
        ReadOnlySpan<char> text) =>
        GetCompletionOption(AllTokensByLineMap, trigger, line, column, text);

    internal static CompletionOption? GetCompletionOption(
        FrozenDictionary<int, ImmutableArray<IToken>> allTokensByLineMap, TextChangeTrigger trigger, int line,
        int column, ReadOnlySpan<char> text)
    {
        if (!allTokensByLineMap.TryGetValue(line, out var tokensAtLine))
        {
            return null;
        }

        CompletionOption? result = GetFileReferenceCompletionOption(tokensAtLine.AsSpan(), text, trigger, column);
        if (result is null)
        {
            result = GetPreprocessorDirectiveCompletionOption(text, trigger, column);
            if (result is null)
            {
                result = GetDirectiveCompletionOption(text, trigger, column);
            }
        }

        return result;
    }
    
    internal static CompletionOption? GetPreprocessorDirectiveCompletionOption(ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var (isMatch, root, replaceableText) = GetPreprocessorDirectiveSuggestion(line, trigger, column);
        if (isMatch)
        {
            return new CompletionOption(CompletionOptionType.PreprocessorDirective, root, false, replaceableText.Length + 1);
        }

        return null;
    }
    
    internal static CompletionOption? GetDirectiveCompletionOption(ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var (isMatch, root, replaceableText) = GetDirectiveSuggestion(line, trigger, column);
        if (isMatch)
        {
            return new CompletionOption(CompletionOptionType.Directive, root, false, replaceableText.Length + 1);
        }

        return null;
    }

    /// <inheritdoc />
    public override ImmutableArray<string> GetPreprocessorDirectiveSuggestions(string root)
    {
        return GetMatches(root.AsSpan(), 1, KickAssemblerLexer.PreprocessorDirectives.AsSpan());
    }
    /// <inheritdoc />
    public override ImmutableArray<string> GetDirectiveSuggestions(string root)
    {
        return GetMatches(root.AsSpan(), 1, KickAssemblerLexer.Directives.AsSpan());
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
    
    [GeneratedRegex("""
                    ^\s*#(?<import>[a-zA-Z]*)$
                    """)]
    internal static partial Regex PreprocessorDirectiveRegex();
    /// <summary>
    /// Returns possible completion for preprocessor directives.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column">Caret column</param>
    /// <returns></returns>
    internal static (bool IsMatch, string Root, string ReplaceableText) GetPreprocessorDirectiveSuggestion(
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        // check obvious conditions
        if (line.Length == 0 || trigger == TextChangeTrigger.CharacterTyped && line[^1] != '#')
        {
            return (false, string.Empty, string.Empty);
        }
        var match = PreprocessorDirectiveRegex().Match(line.ToString());
        if (match.Success)
        {
            int indexOfHash = line.IndexOf('#');
            string root = line[(indexOfHash+1)..(column+1)].ToString();
            return (true, root, match.Groups["import"].Value);
        }
        return (false, string.Empty, string.Empty);
    }

    /// <summary>
    /// Gets whether text left of cursor matched criteria for completion
    /// </summary>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    internal static (bool IsMatch, string Root, string ReplaceableText) GetDirectiveSuggestion(
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        // check obvious conditions
        if (line.Length == 0 || trigger == TextChangeTrigger.CharacterTyped && line[^1] != '.')
        {
            return (false, string.Empty, string.Empty);
        }

        int indexOfDot = column;
        char c;
        while (indexOfDot > 0 && IsValid(line[indexOfDot]))
        {
            indexOfDot--;
        }

        if (indexOfDot < 0 || line[indexOfDot] != '.')
        {
            return (false, string.Empty, string.Empty);
        }
        else
        {
            string root = line[(indexOfDot+1)..(column+1)].ToString();
            int endTextIndex = column + 1;
            while (endTextIndex < line.Length && IsValid(line[endTextIndex]))
            {
                endTextIndex++;
            }
            string entireText = line[(indexOfDot + 1)..endTextIndex].ToString();
            return (true, root, entireText);
        }

        static bool IsValid(char c) => char.IsDigit(c) || char.IsLetter(c) || c is '_';
    }

    /// <summary>
    /// Returns possible completion for file references.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    internal static CompletionOption? GetFileReferenceCompletionOption(ReadOnlySpan<IToken> tokens,
        ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var leftLinePart = line[..(column+1)];
        var (isMatch, doubleQuoteColumn) = GetFileReferenceSuggestion(tokens, leftLinePart, trigger);
        if (isMatch)
        {
            var suggestionLine = line[(doubleQuoteColumn+1)..];
            var (rootText, length, endsWithDoubleQuote) =
                GetSuggestionTextInDoubleQuotes(suggestionLine, column - doubleQuoteColumn);
            return new CompletionOption(CompletionOptionType.FileReference, rootText, endsWithDoubleQuote, length);
        }

        return null;
    }

    /// <summary>
    /// Extracts text of left caret, entire replaceable length and whether it ends with double quote or not
    /// </summary>
    /// <param name="line">Text right to first double quote</param>
    /// <param name="caret">Caret position within line</param>
    /// <returns></returns>
    internal static (string RootText, int Length, bool EndsWithDoubleQuote) GetSuggestionTextInDoubleQuotes(
        ReadOnlySpan<char> line, int caret)
    {
        if (caret > line.Length || caret < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(caret));
        }
        string root = line[..caret].ToString();
        int indexOfDoubleQuote = line.IndexOf('"');
        if (indexOfDoubleQuote < 0)
        {
            return (root, line.Length, false);
        }
        return (root, indexOfDoubleQuote, true);
    }

    /// <summary>
    /// Returns status whether line is valid for file reference suggestions or not.
    /// Also includes index of first double quotes to the left of the column.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="line"></param>
    /// <param name="trigger"></param>
    /// <returns></returns>
    internal static (bool IsMatch, int DoubleQuoteColumn) GetFileReferenceSuggestion(
        ReadOnlySpan<IToken> tokens, ReadOnlySpan<char> line, TextChangeTrigger trigger)
    {
        // check obvious conditions
        if (line.Length == 0 || trigger == TextChangeTrigger.CharacterTyped && line[^1] != '\"')
        {
            return (false, -1);
        }

        int doubleQuoteIndex = line.Length - 1;
        // finds first double quote on the left
        while (doubleQuoteIndex >= 0 && line[doubleQuoteIndex] != '\"')
        {
            doubleQuoteIndex--;
        }

        if (doubleQuoteIndex < 0)
        {
            return (false, -1);
        }
        // there should be only one double quote on the left
        if (line[..doubleQuoteIndex].Contains('\"'))
        {
            return (false, -1);
        }
        // check first token now
        var trimmedLine = TrimWhitespaces(tokens);
        if (trimmedLine.Length == 0)
        {
            return (false, -1);
        }

        var firstToken = trimmedLine[0]; 
        if (firstToken.Type is not (KickAssemblerLexer.HASHIMPORT or KickAssemblerLexer.HASHIMPORTIF))
        {
            return (false, -1);
        }
        var secondToken = trimmedLine[1];
        
        switch (firstToken.Type)
        {
            // when #import tolerate only WS tokens between keyword and double quotes
            case KickAssemblerLexer.HASHIMPORT:
            {
                if (secondToken.Type != KickAssemblerLexer.WS)
                {
                    return (false, -1);
                }
                int start = secondToken.Column + secondToken.Length();
                int end = doubleQuoteIndex - 1;
                if (end > start)
                {
                    foreach (char c in line[start..end])
                    {
                        if (c is not (' ' or '\t'))
                        {
                            return (false, -1);
                        }
                    }
                }

                break;
            }
            case KickAssemblerLexer.HASHIMPORTIF:
                if (secondToken.Type != KickAssemblerLexer.IIF_CONDITION)
                {
                    return (false, -1);
                }
                break;
        }
        return (true, doubleQuoteIndex);
    }

    /// <summary>
    /// Trims WS tokens at start and both WS and EOL at the end of the line. 
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    internal static ReadOnlySpan<IToken> TrimWhitespaces(ReadOnlySpan<IToken> tokens)
    {
        int start = 0;
        while (start < tokens.Length && (tokens[start].Type == KickAssemblerLexer.WS || tokens[start].Channel != 0))
        {
            start++;
        }

        if (start == tokens.Length)
        {
            return ReadOnlySpan<IToken>.Empty;
        }
        int end = tokens.Length - 1;
        while (end >= start &&
               tokens[end].Type is KickAssemblerLexer.WS or KickAssemblerLexer.EOL or KickAssemblerLexer.Eof)
        {
            end--;
        }

        return tokens[start..(end+1)];
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
}