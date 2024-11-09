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
using static Righthand.RetroDbgDataProvider.KickAssembler.Models.KickAssemblerParsedSourceFile;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Models;

public class KickAssemblerParsedSourceFileTest: BaseTest<KickAssemblerParsedSourceFile>
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
            var target = new KickAssemblerParsedSourceFile("fileName", FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
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
            var target = new KickAssemblerParsedSourceFile("fileName", FrozenDictionary<IToken, ReferencedFileInfo>.Empty,
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

            ImmutableArray<LexerBasedSyntaxResult> source =
            [
                new LexerBasedSyntaxResult(0, firstToken, new SyntaxItem(0, 0, TokenType.Number)),
                new LexerBasedSyntaxResult(0, secondToken, new SyntaxItem(0, 0, TokenType.String))
            ];

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source,
                    FrozenDictionary<IToken, string>.Empty);
            
            Assert.That(actual, Is.EquivalentTo(source));
        }
        [Test]
        public void WhenFileReferencesFromParserMatchesSecondToken_SyntaxItemIsReplaced()
        {
            var firstToken = Fixture.Create<IToken>();
            var secondToken = Fixture.Create<IToken>();

            ImmutableArray<LexerBasedSyntaxResult> source =
            [
                new LexerBasedSyntaxResult(0, firstToken, new SyntaxItem(0, 0, TokenType.Number)),
                new LexerBasedSyntaxResult(0, secondToken, new SyntaxItem(0, 0, TokenType.String))
            ];

            var fileReferences = new Dictionary<IToken, string>
            {
                { secondToken, "file.asm" }
            }.ToFrozenDictionary();

            var actual =
                KickAssemblerParsedSourceFile.UpdateLexerAnalysisWithParserAnalysis(source, fileReferences)
                    .ToImmutableArray();

            Assert.That(actual[0], Is.EqualTo(source[0]));
            var item = (FileReferenceSyntaxItem)actual[1].Item;
            Assert.That(item, Is.Not.EqualTo(source[1].Item));
            Assert.That(item.Path, Is.EqualTo("file.asm"));
        }
    }
}