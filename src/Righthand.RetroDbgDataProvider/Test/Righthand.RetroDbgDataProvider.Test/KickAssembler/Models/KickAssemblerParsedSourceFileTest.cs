using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;
using CommonTokenStream = Antlr4.Runtime.CommonTokenStream;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Models;

public class KickAssemblerParsedSourceFileTest : BaseTest<KickAssemblerParsedSourceFile>
{
    readonly DateTimeOffset _lastModified = new DateTimeOffset(2020, 09, 09, 09, 01, 05, TimeSpan.Zero);

    (KickAssemblerLexer Lexer, CommonTokenStream TokenStream, KickAssemblerParser Parser,
        KickAssemblerParserListener ParserListener,
        KickAssemblerLexerErrorListener LexerErrorListener, KickAssemblerParserErrorListener ParserErrorListener)
        GetParsed(string text, params string[] definitions)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitions.ToHashSet(),
        };
        var lexerErrorListener = new KickAssemblerLexerErrorListener();
        var stream = new CommonTokenStream(lexer);
        var parserErrorListener = new KickAssemblerParserErrorListener();
        var parserListener = new KickAssemblerParserListener();
        var parser = new KickAssemblerParser(stream) { BuildParseTree = true };
        parser.AddParseListener(parserListener);
        parser.AddErrorListener(parserErrorListener);
        stream.Fill();
        return (lexer, stream, parser, parserListener, lexerErrorListener, parserErrorListener);
    }

    /// <summary>
    /// Returns all channel tokens.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public ImmutableArray<IToken> GetAllChannelTokens(string code)
    {
        var input = new AntlrInputStream(code);
        var lexer = new KickAssemblerLexer(input);
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        return [..stream.GetTokens()];
    }

    public FrozenDictionary<int, ImmutableArray<IToken>> GetAllChannelTokensByLineMap(string code)
    {
        return GetAllChannelTokens(code)
            .GroupBy(t => t.Line - 1)
            .ToFrozenDictionary(g => g.Key, g => g.OrderBy(t => t.Column).ToImmutableArray());
    }

    [TestFixture]
    public class GetIgnoredDefineContent : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenEmptySource_ReturnsEmptyArray()
        {
            var input = GetParsed("");
            var target = new KickAssemblerParsedSourceFile("fileName",
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
                input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenSourceWithUndefinedContent_ReturnsArrayContainingSingleRange()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #endif
                                  """.FixLineEndings());
            var target = new KickAssemblerParsedSourceFile("fileName",
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
                input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            ImmutableArray<MultiLineTextRange> expected = [new(new TextCursor(2, 0), new TextCursor(2, 10))];
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenSourceWithTwoUndefinedContents_ReturnsArrayContainingBothRanges()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #elif UNDEFINED
                                    yada yada
                                  #endif
                                  """.FixLineEndings());
            var target = new KickAssemblerParsedSourceFile("fileName",
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<SegmentDefinitionInfo>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
                input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);

            var actual = target.GetIgnoredDefineContent(CancellationToken.None);

            ImmutableArray<MultiLineTextRange> expected =
            [
                new(new TextCursor(2, 0), new TextCursor(2, 10)),
                new(new TextCursor(4, 0), new TextCursor(4, 12))
            ];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class UpdateLexerAnalysisWithParserAnalysis : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenNoFileReferencesFromParser_LexerResultsAreReturned()
        {
            var firstToken = Fixture.Create<IToken>();
            var secondToken = Fixture.Create<IToken>();

            ImmutableArray<KickAssemblerParsedSourceFile.LexerBasedSyntaxResult> source =
            [
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, firstToken,
                    new SyntaxItem(0, 0, TokenType.Number)),
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, secondToken,
                    new SyntaxItem(0, 0, TokenType.String))
            ];

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source,
                    FrozenDictionary<IToken, ReferencedFileInfo>.Empty);

            Assert.That(actual, Is.EquivalentTo(source));
        }

        [Test]
        public void WhenFileReferencesFromParserMatchesSecondToken_SyntaxItemIsReplaced()
        {
            var firstToken = Fixture.Create<IToken>();
            var secondToken = Fixture.Create<IToken>();

            ImmutableArray<KickAssemblerParsedSourceFile.LexerBasedSyntaxResult> source =
            [
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, firstToken,
                    new SyntaxItem(0, 0, TokenType.Number)),
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, secondToken,
                    new SyntaxItem(0, 0, TokenType.String))
            ];

            var fileReferences = new Dictionary<IToken, ReferencedFileInfo>
            {
                { secondToken, new ReferencedFileInfo(0, 0, "file.asm", FrozenSet<string>.Empty) }
            }.ToFrozenDictionary();

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source, fileReferences)
                    .ToImmutableArray();

            Assert.That(actual[0], Is.EqualTo(source[0]));
            var item = (FileReferenceSyntaxItem)actual[1].Item;
            Assert.That(item, Is.Not.EqualTo(source[1].Item));
            Assert.That(item.ReferencedFile.RelativeFilePath, Is.EqualTo("file.asm"));
        }
    }

    // [TestFixture]
    // public class GetCompletionOption : KickAssemblerParsedSourceFileTest
    // {
    //     [Test]
    //     public void WhenNoTokens_ReturnsNull()
    //     {
    //         var input = GetParsed("");
    //         
    //         var target = new KickAssemblerParsedSourceFile("fileName", FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
    //             FrozenSet<string>.Empty, FrozenSet<string>.Empty,
    //             _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
    //             input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);
    //
    //         var actual = target.GetCompletionOption(TextChangeTrigger.CompletionRequested, 0);
    //         
    //         Assert.That(actual, Is.Null);
    //     }
    //     [Test]
    //     public void WhenRequestedAndOnlyDoubleQuote_ReturnsNull()
    //     {
    //         var input = GetParsed("\"");
    //         
    //         var target = new KickAssemblerParsedSourceFile("fileName", FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
    //             FrozenSet<string>.Empty, FrozenSet<string>.Empty,
    //             _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
    //             input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);
    //
    //         var actual = target.GetCompletionOption(TextChangeTrigger.CompletionRequested, 1);
    //
    //         Assert.That(actual, Is.Null);
    //     }
    //
    //     [Test]
    //     public void WhenImportAndOnlyDoubleQuote_ReturnsValidOption()
    //     {
    //         var input = GetParsed("#import \"");
    //
    //         var target = new KickAssemblerParsedSourceFile("fileName",
    //             FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
    //             FrozenSet<string>.Empty, FrozenSet<string>.Empty,
    //             _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, input.ParserListener,
    //             input.LexerErrorListener, input.ParserErrorListener, isImportOnce: false);
    //
    //         var actual = target.GetCompletionOption(TextChangeTrigger.CompletionRequested, 9);
    //
    //         Assert.That(actual.Value,
    //             Is.EqualTo(new CompletionOption(CompletionOptionType.FileReference, "", EndsWithDoubleQuote: false)));
    //     }
    // }

    [TestFixture]
    public class GetTokenIndexAtLocation : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenNoSourceCode_ReturnsZero()
        {
            var tokens = GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 0);

            Assert.That(actual, Is.Zero);
        }

        [Test]
        public void WhenLineIsLessThanZero_ReturnsNull()
        {
            var tokens = GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, -1, 0);

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void WhenOffsetIsOutOfBounds_ReturnsNull()
        {
            var tokens = GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 99, 0);

            Assert.That(actual, Is.Null);
        }

        [TestCase(" ")]
        [TestCase("\t")]
        public void WhenHiddenChar_ReturnsOne(string input)
        {
            var tokens = GetAllChannelTokensByLineMap(input);

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 1);

            Assert.That(actual, Is.EqualTo(1));
        }

        [Test]
        public void WhenWithinDefaultToken_ReturnsItsIndex()
        {
            var tokens = GetAllChannelTokensByLineMap("#import \"file.asm\"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 11 - 1);

            Assert.That(actual, Is.EqualTo(2));
        }

        [Test]
        public void WhenRightAfterDoubleQuote_ReturnsIndexOfDoubleQuote()
        {
            var tokens = GetAllChannelTokensByLineMap("#import \"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 9 - 1);

            Assert.That(actual, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class GetFileReferenceCompletionOption : KickAssemblerParsedSourceFileTest
    {
        private (int ZeroBasedColumnIndex, int TokenIndex, ImmutableArray<IToken> Tokens) GetColumnAndTokenIndex(
            string input)
        {
            int zeroBasedColumn = input.IndexOf('|') - 1;

            var tokens = GetAllChannelTokens(input.Replace("|", ""));
            var token = tokens.FirstOrDefault(t => t.StartIndex <= zeroBasedColumn && t.StopIndex >= zeroBasedColumn) ??
                        tokens[^1];
            var tokenIndex = tokens.IndexOf(token);
            return (zeroBasedColumn, tokenIndex, tokens);
        }

        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [TestCase("#import \"|", ExpectedResult = true)]
        [TestCase("  #import \"|", ExpectedResult = true)]
        [TestCase("#import |\"", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif \"|", ExpectedResult = true)]
        [TestCase("  #importif \"|", ExpectedResult = true)]
        [TestCase("#importif |\"", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        [TestCase("#import \"|multi_import.asm\"", ExpectedResult = true)]
        public bool CharacterTypedCases(string input)
        {
            var (zeroBasedColumn, _, tokens) = GetColumnAndTokenIndex(input);

            var actual =
                KickAssemblerParsedSourceFile.GetFileReferenceCompletionOption(tokens.AsSpan(), input.Replace("|", ""),
                    TextChangeTrigger.CharacterTyped,
                    zeroBasedColumn);

            return actual?.Type == CompletionOptionType.FileReference;
        }

        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [TestCase("#import \"|", ExpectedResult = true)]
        [TestCase("  #import \"|", ExpectedResult = true)]
        [TestCase("#import |\"", ExpectedResult = false)]
        [TestCase("#import x \"|", ExpectedResult = false)]
        [TestCase("#importif \"|", ExpectedResult = true)]
        [TestCase("  #importif \"|", ExpectedResult = true)]
        [TestCase("#importif |\"", ExpectedResult = false)]
        [TestCase("#importif x \"|", ExpectedResult = true)]
        [TestCase("#import \"|multi_import.asm\"", ExpectedResult = true)]
        [TestCase("#import \"multi|_import.asm\"", ExpectedResult = true)]
        [TestCase("#import \"multi_import.as|m\"", ExpectedResult = true)]
        [TestCase("#import \"multi_import.as|", ExpectedResult = true)]
        public bool CompletionRequestedTypedCases(string input)
        {
            var (zeroBasedColumn, tokenIndex, tokens) = GetColumnAndTokenIndex(input);

            var actual =
                KickAssemblerParsedSourceFile.GetFileReferenceCompletionOption(tokens.AsSpan(), input,
                    TextChangeTrigger.CompletionRequested,
                    zeroBasedColumn);

            return actual?.Type == CompletionOptionType.FileReference;
        }
    }

    [TestFixture]
    public class GetFileReferenceSuggestion : KickAssemblerParsedSourceFileTest
    {
        /// <summary>
        /// | signifies the caret position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        [TestCase("#import \"", TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        [TestCase("#import \"xxx", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#import \" \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#importx \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#import x \"", TextChangeTrigger.CharacterTyped, ExpectedResult = false)]
        [TestCase("#importif x \"", TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        public bool GivenSample_ReturnsCorrectMatchCriteria(string input, TextChangeTrigger trigger)
        {
            var tokens = GetAllChannelTokens(input);

            var actual = KickAssemblerParsedSourceFile.GetFileReferenceSuggestion(tokens.AsSpan(), input, trigger);

            return actual.IsMatch;
        }
    }

    [TestFixture]
    public class TrimWhitespaces : KickAssemblerParsedSourceFileTest
    {
        [TestCaseSource(typeof(GivenSampleTrimsProperlySource))]
        public void GivenSample_TrimsProperly(ImmutableArray<IToken> data, ImmutableArray<IToken> expected)
        {
            var actual = KickAssemblerParsedSourceFile.TrimWhitespaces(data.AsSpan());

            Assert.That(actual.ToImmutableArray(), Is.EquivalentTo(expected));
        }

        public class GivenSampleTrimsProperlySource : IEnumerable
        {
            private static ImmutableArray<IToken> GetArray(
                params (int TokenType, int Channel)[] tokens) =>
                [..tokens.Select(t => new TrimWhitespacesToken(t.TokenType, t.Channel))];

            public IEnumerator GetEnumerator()
            {
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.STRING, 0)),
                    GetArray((KickAssemblerLexer.STRING, 0)));
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.WS, 0)),
                    GetArray());
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.WS, 0), (KickAssemblerLexer.STRING, 0), (KickAssemblerLexer.EOL, 1)),
                    GetArray((KickAssemblerLexer.STRING, 0)));
            }
        }

        [TestFixture]
        public class GetSuggestionTextInDoubleQuotes : KickAssemblerParsedSourceFileTest
        {
            [TestCase("", 0, ExpectedResult = "")]
            [TestCase("xys", 0, ExpectedResult = "")]
            [TestCase("xys", 1, ExpectedResult = "x")]
            [TestCase("xys", 2, ExpectedResult = "xy")]
            [TestCase("xys", 3, ExpectedResult = "xys")]
            [TestCase("xys\"", 3, ExpectedResult = "xys")]
            public string GivenSample_ReturnsCorrectRoot(string input, int caret)
            {
                return KickAssemblerParsedSourceFile.GetSuggestionTextInDoubleQuotes(input, caret).RootText;
            }

            [TestCase("", 0, ExpectedResult = 0)]
            [TestCase("xys", 0, ExpectedResult = 3)]
            [TestCase("xys", 2, ExpectedResult = 3)]
            [TestCase("xys", 3, ExpectedResult = 3)]
            [TestCase("xys\"", 3, ExpectedResult = 3)]
            public int GivenSample_ReturnsCorrectLength(string input, int caret)
            {
                return KickAssemblerParsedSourceFile.GetSuggestionTextInDoubleQuotes(input, caret).Length;
            }

            [TestCase("xys", 3, ExpectedResult = false)]
            [TestCase("xys\"", 3, ExpectedResult = true)]
            [TestCase("xys \t\"", 3, ExpectedResult = true)]
            public bool GivenSample_ReturnsCorrectEndsWithDoubleQuote(string input, int caret)
            {
                return KickAssemblerParsedSourceFile.GetSuggestionTextInDoubleQuotes(input, caret).EndsWithDoubleQuote;
            }
        }

        public record TrimWhitespacesToken : IToken
        {
            public string Text => "N/A";
            public int Type { get; }
            public int Line => -1;
            public int Column => -1;
            public int Channel { get; }
            public int TokenIndex => -1;
            public int StartIndex => -1;
            public int StopIndex => -1;
            public ITokenSource TokenSource => null!;
            public ICharStream InputStream => null!;

            public TrimWhitespacesToken(int type, int channel)
            {
                Type = type;
                Channel = channel;
            }
        }
    }

    [TestFixture]
    public class TestTrackImport : KickAssemblerParsedSourceFileTest
    {
        private KickAssemblerDbgParser _parser = default!;
        private KickAssemblerProgramInfoBuilder _infoBuilder = default!;
        private DbgData _dbg = default!;

        [SetUp]
        public void Setup()
        {
            _parser = new KickAssemblerDbgParser(Substitute.For<ILogger<KickAssemblerDbgParser>>());
            _infoBuilder =
                new KickAssemblerProgramInfoBuilder(Substitute.For<ILogger<KickAssemblerProgramInfoBuilder>>());
        }

        [Test]
        public async Task Test()
        {
            var sample = LoadKickAssSampleFile("SimpleImport", "main.dbg");
            _dbg = await _parser.LoadContentAsync(sample, "path");
            var assemblyInfo = await _infoBuilder.BuildAppInfoAsync("path", _dbg);
        }
    }

    [TestFixture]
    public class GetPreprocessorDirectiveSuggestion : KickAssemblerParsedSourceFileTest
    {
        [TestCase("#", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        [TestCase(" #", 1, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        public bool GivenLine_ReturnsIsMatch(string line, int column, TextChangeTrigger trigger)
        {
            return KickAssemblerParsedSourceFile.GetPreprocessorDirectiveSuggestion(line, trigger, column).IsMatch;
        }

        [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        public string GivenLine_ReturnsRoot(string line, int column, TextChangeTrigger trigger)
        {
            return KickAssemblerParsedSourceFile.GetPreprocessorDirectiveSuggestion(line, trigger, column).Root;
        }

        [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "import")]
        public string GivenLine_ReturnsReplaceableText(string line, int column, TextChangeTrigger trigger)
        {
            return KickAssemblerParsedSourceFile.GetPreprocessorDirectiveSuggestion(line, trigger, column)
                .ReplaceableText;
        }
    }

    [TestFixture]
    public class GetMatches : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void WhenEmptyList_FindsNoMatches()
        {
            var actual = KickAssemblerParsedSourceFile.GetMatches("ab", 1, ReadOnlySpan<string>.Empty);

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenValidRoot_FindsAllMatches()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("ab", 1, list.AsSpan());

            ImmutableArray<string> expected = ["#abb", "#abc"];
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenRootHasNoMatched_ReturnsEmptyList()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("x", 1, list.AsSpan());

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void WhenRootDoesNotStartAfterHash_ReturnsEmptyList()
        {
            ImmutableArray<string> list = ["#a", "#abb", "#abc", "#d"];

            var actual = KickAssemblerParsedSourceFile.GetMatches("b", 1, list.AsSpan());

            Assert.That(actual, Is.Empty);
        }
    }

    [TestFixture]
    public class IsCursorWithinArray : KickAssemblerParsedSourceFileTest
    {
        [TestCase(".file [name=\"")]
        [TestCase(".file [segments=\"Code\",name=\"")]
        [TestCase(".file [segments = \"Code\" , name = \"")]
        [TestCase(".segment Base [prgFiles = \"")]
        [TestCase(".segment Base [prgFiles=\"test.prg,")]
        [TestCase("zpCode: .segment Base [prgFiles=\"test.prg,")]
        public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNonNullResult(string line)
        {
            var actual =
                KickAssemblerParsedSourceFile.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
                    KickAssemblerParsedSourceFile.ValuesCount.Multiple);

            Assert.That(actual, Is.Not.Null);
        }

        [TestCase(".file [name=\"\"")]
        [TestCase(".file [segments=\"Code\" name=\"")]
        [TestCase(".file segments = \"Code\" , name = \"")]
        [TestCase("zpCode: .file segments = \"Code\" , name = \"")]
        public void GivenSampleInputThatDoesNotPutCursorWithinArray_ReturnsNullResult(string line)
        {
            var actual =
                KickAssemblerParsedSourceFile.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
                    KickAssemblerParsedSourceFile.ValuesCount.Multiple);

            Assert.That(actual, Is.Null);
        }

        [TestCase(".file [name=\"", ExpectedResult = 6)]
        [TestCase(".file [segments=\"Code\",name=\"", ExpectedResult = 6)]
        [TestCase(".file  [ segments = \"Code\" , name = \"", ExpectedResult = 7)]
        [TestCase("zpCode: .file  [ segments = \"Code\" , name = \"", ExpectedResult = 15)]
        public int? GivenSampleInput_ReturnsOpenBracketColumnIndex(string line)
        {
            return KickAssemblerParsedSourceFile
                .IsCursorWithinArray(line, 0, line.Length, line.Length-1,
                    KickAssemblerParsedSourceFile.ValuesCount.Multiple)?.OpenBracketColumn;
        }

        [Test]
        public void WhenNotSupportingMultipleValues_AndCommaDelimitedValueIsInFront_ReturnsNull()
        {
            string line = ".file [name=\"value,";
            var actual =
                KickAssemblerParsedSourceFile.IsCursorWithinArray(line, 0, line.Length, line.Length-1,
                    KickAssemblerParsedSourceFile.ValuesCount.Single);

            Assert.That(actual, Is.Null);
        }
    }

    [TestFixture]
    public class FindFirstArrayDelimiterPosition : KickAssemblerParsedSourceFileTest
    {
        [TestCase("", 0, ExpectedResult = null)]
        [TestCase("tubo,", 0, ExpectedResult = 4)]
        [TestCase("tubo,", 2, ExpectedResult = 4)]
        [TestCase("tubo , ", 2, ExpectedResult = 5)]
        [TestCase("tubo , \"", 2, ExpectedResult = 5)]
        [TestCase("tubo \" ,", 2, ExpectedResult = 5)]
        [TestCase("tubo", 2, ExpectedResult = null)]
        public int? GivenSampleInput_ReturnsCorrectResult(string line, int cursor)
        {
            return KickAssemblerParsedSourceFile.FindFirstArrayDelimiterPosition(line, cursor);
        }
    }

    [TestFixture]
    public class GetArrayValues : KickAssemblerParsedSourceFileTest
    {
        [TestCase("", ExpectedResult = new string[0])]
        [TestCase("\"alfa.prg", ExpectedResult = new string[] { "alfa.prg" })]
        [TestCase("\"alfa.prg,", ExpectedResult = new string[] { "alfa.prg" })]
        [TestCase("\"alfa.prg,a.prg", ExpectedResult = new string[] { "alfa.prg","a.prg" })]
        [TestCase("\"alfa.prg,a.prg\"", ExpectedResult = new string[] { "alfa.prg","a.prg" })]
        public string[] GivenSampleInput_ReturnsCorrectArray(string text)
        {
            return KickAssemblerParsedSourceFile.GetArrayValues(text, 0, text.Length)
                .ToArray();
        }
    }

    [TestFixture]
    public class GetCurrentArrayValue : KickAssemblerParsedSourceFileTest
    {
        [TestCase("", null, ExpectedResult = "")]
        [TestCase("alfa", null, ExpectedResult = "alfa")]
        [TestCase(" alfa", null, ExpectedResult = "alfa")]
        [TestCase(" alfa, ", null, ExpectedResult = "alfa")]
        [TestCase(" alfa\"", null, ExpectedResult = "alfa")]
        [TestCase(" alfa\r\nx", 5, ExpectedResult = "alfa")]
        public string? GivenSampleInput_ReturnsCurrentValue(string line, int? length)
        {
            return KickAssemblerParsedSourceFile.GetCurrentArrayValue(line, 0, length ?? line.Length);
        }
    }
}