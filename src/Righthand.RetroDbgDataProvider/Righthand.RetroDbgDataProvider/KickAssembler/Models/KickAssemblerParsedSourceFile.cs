using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;
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
            (GetFileReferenceCompletionOption(tokensAtLine.AsSpan(), textSpan, trigger, column) ??
             GetPreprocessorDirectiveCompletionOption(textSpan, trigger, column)) ??
            GetFileSuggestionInArrayCompletionOption(tokensAtLine.AsSpan(), text, textStart, textLength, trigger,
                column,
                ValuesCount.Multiple);

        return result;
    }

    
    internal static CompletionOption? GetPreprocessorDirectiveCompletionOption(ReadOnlySpan<char> line, TextChangeTrigger trigger, int column)
    {
        var (isMatch, root, replaceableText) = GetPreprocessorDirectiveSuggestion(line, trigger, column);
        if (isMatch)
        {
            return new CompletionOption(CompletionOptionType.PreprocessorDirective, root, false, replaceableText.Length + 1, []);
        }

        return null;
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
    
    [GeneratedRegex("""
                    ^\s*#(?<import>[a-zA-Z]*)$
                    """, RegexOptions.Singleline)]
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
            return new CompletionOption(CompletionOptionType.FileReference, rootText, endsWithDoubleQuote, length, []);
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

    /// <summary>
    /// Generic file suggestion completion.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="text"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineLength"></param>
    /// <param name="trigger"></param>
    /// <param name="column"></param>
    /// <param name="valuesCountSupport"></param>
    /// <returns></returns>
    /// <remarks>
    /// Handles such cases:
    /// .file [name="test.prg", segments="Code", sidFiles="file.sid"]
    /// .segment Base [prgFiles="basefile.prg"]
    /// .segmentdef Misc1 [prgFiles="data/Music.prg, data/Charset2x2.prg"]
    /// *** .import c64 "data/Music.prg"
    /// .segment Main [sidFiles="data/music.sid", outPrg="out.prg"]
    ///
    /// Where files = "file(, file)*"
    /// </remarks>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static CompletionOption? GetFileSuggestionInArrayCompletionOption(ReadOnlySpan<IToken> tokens,
        string text, int lineStart, int lineLength, TextChangeTrigger trigger, int column, ValuesCount valuesCountSupport)
    {
        var line = text.AsSpan()[lineStart..(lineStart + lineLength)];
        if (line.Length == 0)
        {
            return null;
        }

        // TODO properly handle valuesCountSupport (to limit it to single value when required)
        var cursorWithinArray = IsCursorWithinArray(text, lineStart, lineLength, column, valuesCountSupport);
        if (cursorWithinArray is not null)
        {
            CompletionOptionType? completionOptionType = cursorWithinArray.Value.ArgumentName switch
            {
                "sidFiles" => CompletionOptionType.SidFile,
                "prgFiles" or "name" => CompletionOptionType.ProgramFile,
                "segments" => CompletionOptionType.Segments,
                _ => null,
            };
            if (completionOptionType is not null)
            {
                CompletionOption? completionOption = completionOptionType switch
                {
                    CompletionOptionType.SidFile when cursorWithinArray.Value.KeyWord is ".segment" or ".segmentdef"
                            or ".segmentout"
                            or "file" =>
                        new CompletionOption(completionOptionType.Value, cursorWithinArray.Value.Root,
                            cursorWithinArray.Value.HasEndDelimiter, cursorWithinArray.Value.ReplacementLength,
                            cursorWithinArray.Value.ArrayValues),
                    CompletionOptionType.ProgramFile when cursorWithinArray.Value.KeyWord is ".segment" or ".segmentdef"
                            or ".segmentout"
                        =>
                        new CompletionOption(completionOptionType.Value, cursorWithinArray.Value.Root,
                            cursorWithinArray.Value.HasEndDelimiter, cursorWithinArray.Value.ReplacementLength,
                            cursorWithinArray.Value.ArrayValues),
                    CompletionOptionType.Segments when cursorWithinArray.Value.KeyWord is ".file" or ".segmentdef"
                        or ".segmentout" => GetCompletionOptionForSegments(completionOptionType.Value,
                        cursorWithinArray.Value),

                    _ => null,
                };
                return completionOption;
            }
        }

        return null;
    }

    private static CompletionOption GetCompletionOptionForSegments(CompletionOptionType completionOptionType,
        IsCursorWithinArrayResult data)
    {
        ImmutableArray<string> excludedValues;
        if (data.KeyWord.Equals(".segmentdef", StringComparison.Ordinal) && data.Parameter is not null)
        {
            excludedValues = data.ArrayValues.Add(data.Parameter);
        }
        else
        {
            excludedValues = data.ArrayValues;
        }

        return new CompletionOption(completionOptionType, data.Root, data.HasEndDelimiter, data.ReplacementLength,
            excludedValues);
    }

    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex("""
                    (?<KeyWord>(\.segmentdef|\.segment|.segmentout|\.file))\s*(?<Parameter>\w+)?\s*(?<OpenBracket>\[)\s*((?<PrevArgName>\w+)\s*(=\s*((?<PrevQuotedValue>".*")|(?<PrevUnquotedValue>[^,\s]+)))?\s*,\s*)*(?<ArgName>\w+)\s*=\s*(?<StartDoubleQuote>")(\s*(?<PrevArrayItem>[^,"]*)\s*(?<ArgComma>,))*\s*(?<Root>[^,"]*)$
                    """, RegexOptions.Singleline)]
    private static partial Regex ArraySuggestionTemplateRegex();
    /// <summary>
    /// Finds whether cursor is within array of options. KeyWord is limited to .segmentDef, .segment and .file
    /// </summary>
    /// <param name="text"></param>
    /// <param name="lineStart">Line start in absolute index</param>
    /// <param name="lineLength"></param>
    /// <param name="cursor">Cursor position in absolute index</param>
    /// <param name="valuesCountSupport">When Multiple, multiple comma-delimited values are supported, only a single value otherwise</param>
    /// <returns></returns>
    /// <remarks>
    /// Groups:
    /// - StartDoubleQuote is starting double quote of values
    /// </remarks>
    internal static IsCursorWithinArrayResult? IsCursorWithinArray(string text, int lineStart, int lineLength,
        int cursor, ValuesCount valuesCountSupport)
    { 
        Debug.WriteLine($"Searching IsCursorWithinArrayResult in: '{text.Substring(lineStart, cursor+1)}'");
        int lineEnd = lineStart + lineLength;
        // tries to match against text left of cursor
        var match = ArraySuggestionTemplateRegex().Match(text, lineStart, cursor+1);
        if (match.Success)
        {
            // when supports only a single value, can't have comma-separated values in front 
            if (valuesCountSupport == ValuesCount.Single && match.Groups["PrevArrayItem"].Success)
            {
                Debug.WriteLine("Doesn't support multiple values and comma was found");
                return null;
            }

            var line = text.AsSpan()[lineStart..lineEnd];
            int? firstDelimiterColumn = FindFirstArrayDelimiterPosition(line, cursor+1) + lineStart;
            var rootGroup = match.Groups["Root"];
            int startDoubleQuote = match.Groups["StartDoubleQuote"].Index;
            var arrayValues = GetArrayValues(text, startDoubleQuote, lineEnd - startDoubleQuote);
            var currentValue = GetCurrentArrayValue(text, rootGroup.Index, lineEnd);
            Debug.WriteLine($"Found a match with current being '{currentValue}' and array values {string.Join(",", arrayValues.Select(a => $"'{a}'"))}");
            
            return new IsCursorWithinArrayResult(
                match.Groups["KeyWord"].Value,
                match.Groups["Parameter"].Value,
                match.Groups["ArgName"].Value,
                rootGroup.Value,
                match.Groups["OpenBracket"].Index,
                ReplacementLength: currentValue.Length,
                HasEndDelimiter: firstDelimiterColumn is not null,
                arrayValues
            );
        }
        else
        {
            Debug.WriteLine("Doesn't match");
        }

        return null;
    }

    [GeneratedRegex("""
                    ^\s*(?<Item>[^,"]*)
                    """, RegexOptions.Singleline)]
    private static partial Regex GetCurrentArrayValueRegex();
    internal static string GetCurrentArrayValue(string text, int start, int end)
    {
        var match = GetCurrentArrayValueRegex().Match(text, start, end-start);
        if (match.Success)
        {
            return match.Groups["Item"].Value;
        }

        throw new Exception("Shouldn't happen");
    }

    [GeneratedRegex("""
                    ^"(\s*(?<ArrayItem>[^,"]*)\s*,)*\s*(?<LastItem>[^,"]*)"?
                    """, RegexOptions.Singleline)]
    private static partial Regex GetArrayValuesRegex();
    internal static ImmutableArray<string> GetArrayValues(string text, int start, int length)
    {
        var m = GetArrayValuesRegex().Match(text, start, length);
        if (m.Success)
        {
            var items = m.Groups["ArrayItem"].Captures
                .Where(c => !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .ToImmutableArray();
            string? lastItem = m.Groups["LastItem"].Value;
            if (!string.IsNullOrWhiteSpace(lastItem))
            {
                return items.Add(lastItem);
            }

            return items;
        }

        return [];
    }

    internal static int? FindFirstArrayDelimiterPosition(ReadOnlySpan<char> line, int cursor)
    {
        int delimiterPosition = cursor;
        while (delimiterPosition < line.Length)
        {
            if (line[delimiterPosition] is ',' or '"')
            {
                return delimiterPosition;
            }
            delimiterPosition++;
        }

        return null;
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    internal record struct IsCursorWithinArrayResult(
        string KeyWord,
        string? Parameter,
        string ArgumentName,
        string Root,
        int OpenBracketColumn,
        int ReplacementLength,
        bool HasEndDelimiter,
        ImmutableArray<string> ArrayValues);

    public enum ValuesCount
    {
        Single,
        Multiple,
    }
}