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
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 0);

            Assert.That(actual, Is.Zero);
        }

        [Test]
        public void WhenLineIsLessThanZero_ReturnsNull()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, -1, 0);

            Assert.That(actual, Is.Null);
        }

        [Test]
        public void WhenOffsetIsOutOfBounds_ReturnsNull()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 99, 0);

            Assert.That(actual, Is.Null);
        }

        [TestCase(" ")]
        [TestCase("\t")]
        public void WhenHiddenChar_ReturnsOne(string input)
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap(input);

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 1);

            Assert.That(actual, Is.EqualTo(1));
        }

        [Test]
        public void WhenWithinDefaultToken_ReturnsItsIndex()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("#import \"file.asm\"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 11 - 1);

            Assert.That(actual, Is.EqualTo(2));
        }

        [Test]
        public void WhenRightAfterDoubleQuote_ReturnsIndexOfDoubleQuote()
        {
            var tokens = AntlrTestUtils.GetAllChannelTokensByLineMap("#import \"");

            var actual = KickAssemblerParsedSourceFile.GetTokenIndexAtLocation(tokens, 0, 9 - 1);

            Assert.That(actual, Is.EqualTo(2));
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
}