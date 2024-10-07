using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class LexerTest
{
    ImmutableArray<IToken> GetTokens(string text, params string[] definitons)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input)
        {
            DefinedSymbols = definitons.ToHashSet(),
        };
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [..stream.GetTokens().Where(t => t.Channel == 0)];
    }

    ImmutableArray<int> GetTokenTypes(params int[] tokens)
    {
        return [..tokens];
    }
    [TestFixture]
    public class HashIf : LexerTest
    {
        [Test]
        public void WhenSimpleIfWithOutsideDefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens(input, "DEFINED");

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
                );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithInsideDefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #define DEFINED
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHDEFINE, KickAssemblerLexer.DEFINED_TOKEN,
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN,
                //KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfWithUndefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED
                                    #if UNDEFINED
                                        lda #5
                                    #endif
                                 #endif
                                 """;
            
            var actual  = GetTokens(input, "DEFINED");

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
    }
}

public static class LexerTestExtensions
{
    public static ImmutableArray<int> GetTokenTypes(this ImmutableArray<IToken> tokens)
    {
        return [..tokens.Select(t => t.Type)];
    }
}