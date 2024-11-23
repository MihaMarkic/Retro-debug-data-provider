using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using AutoFixture;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

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
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
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
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
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
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
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
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, firstToken, new SyntaxItem(0, 0, TokenType.Number)),
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, secondToken, new SyntaxItem(0, 0, TokenType.String))
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
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, firstToken, new SyntaxItem(0, 0, TokenType.Number)),
                new KickAssemblerParsedSourceFile.LexerBasedSyntaxResult(0, secondToken, new SyntaxItem(0, 0, TokenType.String))
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
    public class IsFileReferenceCompletionOption : KickAssemblerParsedSourceFileTest
    {
        [Test]
        public void GivenSampleMatchWithinStringAndCompletionRequested_ReturnsCorrectCompletionOption()
        {
            var tokens = GetAllChannelTokens("#import \"tubo.\"");

            var actual =
                KickAssemblerParsedSourceFile.IsFileReferenceCompletionOption(tokens.AsSpan(),
                    TextChangeTrigger.CompletionRequested, 2, 11);

            Assert.That(actual,
                Is.EqualTo(new CompletionOption(CompletionOptionType.FileReference, "tu", EndsWithDoubleQuote: true, 5)));
        }

        [Test]
        public void
            GivenSampleMatchWithinStringNotEndingWithDoubleQuoteAndCompletionRequested_ReturnsCorrectCompletionOption()
        {
            var tokens = GetAllChannelTokens("#import \"tubo.");

            var actual =
                KickAssemblerParsedSourceFile.IsFileReferenceCompletionOption(tokens.AsSpan(),
                    TextChangeTrigger.CompletionRequested, 3, 11);

            Assert.That(actual,
                Is.EqualTo(new CompletionOption(CompletionOptionType.FileReference, "tu", EndsWithDoubleQuote: false, 5)));
        }

        [Test]
        public void GivenDoubleQuoteAndCharacterTyped_ReturnsCorrectCompletionOption()
        {
            var tokens = GetAllChannelTokens("#import \"");

            var actual =
                KickAssemblerParsedSourceFile.IsFileReferenceCompletionOption(tokens.AsSpan(), TextChangeTrigger.CharacterTyped,
                    2, 9 - 1);

            Assert.That(actual,
                Is.EqualTo(new CompletionOption(CompletionOptionType.FileReference, string.Empty,
                    EndsWithDoubleQuote: false, 0)));
        }

        private (int ZeroBasedColumnIndex, int TokenIndex, ImmutableArray<IToken> Tokens) GetColumnAndTokenIndex(string input)
        {
            int zeroBasedColumn = input.IndexOf('|')-1;

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
            var (zeroBasedColumn, tokenIndex, tokens) = GetColumnAndTokenIndex(input);

            var actual =
                KickAssemblerParsedSourceFile.IsFileReferenceCompletionOption(tokens.AsSpan(), TextChangeTrigger.CharacterTyped,
                    tokenIndex, zeroBasedColumn);

            return actual?.Type == CompletionOptionType.FileReference;
        }

        [Test]
        public void GivenSampleMatchWhenNoImport_ReturnsNull()
        {
            var tokens = GetAllChannelTokens("#if \"tubo.\"");

            var actual =
                KickAssemblerParsedSourceFile.IsFileReferenceCompletionOption(tokens.AsSpan(),
                    TextChangeTrigger.CompletionRequested, 2, 12 - 1);

            Assert.That(actual, Is.Null);
        }
    }

    [TestFixture]
    public class IsImportIfCommand : KickAssemblerParsedSourceFileTest
    {
        [TestCase("#importif ", ExpectedResult = true)]
        [TestCase(" #importif ", ExpectedResult = true)]
        [TestCase("\t#importif ", ExpectedResult = true)]
        [TestCase("#importif dsfsd dfds > ", ExpectedResult = true)]
        [TestCase("#importif dsfsd \" dfds > ", ExpectedResult = false)]
        [TestCase("#importif \"", ExpectedResult = false)]
        [TestCase("  #importif \"", ExpectedResult = false)]
        public bool GivenSample_ReturnsCorrectValue(string input)
        {
            var tokens = GetAllChannelTokens(input);

            return KickAssemblerParsedSourceFile.IsImportIfCommand(tokens.AsSpan(), tokens.Length-1);
        }
    }

    [TestFixture]
    public class GetReplaceableTextLength : KickAssemblerParsedSourceFileTest
    {
        [TestCase("#import \"tubo.\"", 2, ExpectedResult = 5)]
        [TestCase("#import \"tubo.", 3, ExpectedResult = 5)]
        [TestCase("#import \"", 3, ExpectedResult = 0)]
        [TestCase("""
                  #import "
                  lda 5
                  """, 
            3, ExpectedResult = 0)]
        [TestCase("""
                  #import "one_main.asm
                  lda 5
                  """, 
            3, ExpectedResult = 12)]
        public int GivenSample_ReturnsCorrectLength(string input, int startTokenIndex)
        {
            var tokens = GetAllChannelTokens(input);
            
            var actual = KickAssemblerParsedSourceFile.GetReplaceableTextLength(tokens.AsSpan()[startTokenIndex..]);

            return actual.Length;
        }
        [TestCase("#import \"tubo.\"", 2, ExpectedResult = true)]
        [TestCase("#import \"tubo.", 3, ExpectedResult = false)]
        public bool GivenSample_ReturnsCorrectEndWithQuotes(string input, int startTokenIndex)
        {
            var tokens = GetAllChannelTokens(input);
            
            var actual = KickAssemblerParsedSourceFile.GetReplaceableTextLength(tokens.AsSpan()[startTokenIndex..]);

            return actual.EndsWithDoubleQuote;
        }
    }
}