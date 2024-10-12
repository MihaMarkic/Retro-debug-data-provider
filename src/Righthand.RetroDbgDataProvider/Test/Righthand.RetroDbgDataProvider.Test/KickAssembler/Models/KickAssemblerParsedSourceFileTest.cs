using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Models;

public class KickAssemblerParsedSourceFileTest
{
    
    [TestFixture]
    public class GetIgnoredDefineContent : KickAssemblerParsedSourceFileTest
    {
        readonly DateTimeOffset _lastModified = new DateTimeOffset(2020, 09, 09, 09, 01, 05, TimeSpan.Zero);
        (KickAssemblerLexer Lexer, CommonTokenStream TokenStream, KickAssemblerParser Parser) GetParsed(string text, params string[] definitons)
        {
            var input = new AntlrInputStream(text);
            var lexer = new KickAssemblerLexer(input)
            {
                DefinedSymbols = definitons.ToHashSet(),
            };
            var stream = new CommonTokenStream(lexer);
            var parser = new KickAssemblerParser(stream) { BuildParseTree = true };
            stream.Fill();
            return (lexer, stream, parser);
        }
        
        [Test]
        public void WhenEmptySource_ReturnsEmptyArray()
        {
            var input = GetParsed("");
            var target = new KickAssemblerParsedSourceFile("fileName", FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<string>.Empty, 
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, isImportOnce: false);
            
            var actual = target.GetIgnoredDefineContent();
            
            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void WhenSourceWithUndefinedContent_ReturnsArrayContainingSingleRange()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #endif
                                  """);
            var target = new KickAssemblerParsedSourceFile("fileName", FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<string>.Empty, 
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, isImportOnce: false);
            
            var actual = target.GetIgnoredDefineContent();

            ImmutableArray<TextRange> expected = [new(new TextCursor(1, 13), new TextCursor(2, 11))];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSourceWithTwoUndefinedContents_ReturnsArrayContainingBothRange()
        {
            var input = GetParsed("""
                                  #if UNDEFINED
                                    bla bla
                                  #elif UNDEFINED
                                    yada yada
                                  #endif
                                  """);
            var target = new KickAssemblerParsedSourceFile("fileName", FrozenSet<string>.Empty, FrozenSet<string>.Empty, FrozenSet<string>.Empty, 
                _lastModified, liveContent: null, input.Lexer, input.TokenStream, input.Parser, isImportOnce: false);
            
            var actual = target.GetIgnoredDefineContent();

            ImmutableArray<TextRange> expected = [
                new(new TextCursor(1, 13), new TextCursor(2, 11)),
                new(new TextCursor(3, 15), new TextCursor(4, 13))
            ];
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}