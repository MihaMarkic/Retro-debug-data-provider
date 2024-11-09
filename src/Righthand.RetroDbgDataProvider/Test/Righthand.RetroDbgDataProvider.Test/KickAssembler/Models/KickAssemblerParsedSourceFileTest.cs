using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Models;

public class KickAssemblerParsedSourceFileTest
{
    [TestFixture]
    public class GetIgnoredDefineContent : KickAssemblerParsedSourceFileTest
    {
        readonly DateTimeOffset _lastModified = new DateTimeOffset(2020, 09, 09, 09, 01, 05, TimeSpan.Zero);

        (KickAssemblerLexer Lexer, CommonTokenStream TokenStream, KickAssemblerParser Parser,
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
            var parser = new KickAssemblerParser(stream) { BuildParseTree = true };
            parser.AddErrorListener(parserErrorListener);
            stream.Fill();
            return (lexer, stream, parser, lexerErrorListener, parserErrorListener);
        }

        [Test]
        public void WhenEmptySource_ReturnsEmptyArray()
        {
            var input = GetParsed("");
            var target = new KickAssemblerParsedSourceFile("fileName", ImmutableArray<ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser,
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
            var target = new KickAssemblerParsedSourceFile("fileName", ImmutableArray<ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser,
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
            var target = new KickAssemblerParsedSourceFile("fileName", ImmutableArray<ReferencedFileInfo>.Empty,
                FrozenSet<string>.Empty, FrozenSet<string>.Empty,
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser,
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
}