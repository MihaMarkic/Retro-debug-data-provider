using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class LexerTest
{
    ImmutableArray<IToken> GetTokens(string text)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return [..stream.GetTokens()];
    }

    ImmutableArray<int> GetTokenTypes(params int[] tokens)
    {
        return [..tokens];
    }
    [TestFixture]
    public class HashIf : LexerTest
    {
        [Test]
        public void WhenSimpleIfWithDefinedCondition_ReturnsAllTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASH, KickAssemblerLexer.IF, KickAssemblerLexer.UNQUOTED_STRING, KickAssemblerLexer.EOL,
                KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASH, KickAssemblerLexer.ENDIF, KickAssemblerLexer.EOL
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