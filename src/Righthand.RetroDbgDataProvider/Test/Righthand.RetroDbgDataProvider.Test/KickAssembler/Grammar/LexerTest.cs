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
        var tokens = stream.GetTokens();
        return [..tokens.Where(t => t.Channel == 0)];
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
        public void WhenSimpleIfElseWithIfDefinedCondition_ReturnsIfTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    ldy #8
                                 #endif
                                 """;
            
            var actual  = GetTokens(input, "DEFINED");

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHELSE,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfElseWithIfUndefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    ldy #8
                                 #endif
                                 """;
            
            var actual  = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHELSE,
                    KickAssemblerLexer.LDY, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfElseWithIfUndefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    #if DEFINED2
                                        ldy #8
                                    #endif
                                 #endif
                                 """;

            var actual = GetTokens(input, "DEFINED2");

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHELSE,
                    KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                        KickAssemblerLexer.LDY, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHENDIF, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenNestedIfElseWithIfDynamicallyDefinedCondition_ReturnsElseTokens()
        {
            const string input = """
                                 #if DEFINED
                                    lda #5
                                 #else
                                    #define DEFINED2
                                    #if DEFINED2
                                        ldy #8
                                    #endif
                                 #endif
                                 """;

            var actual = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHELSE,
                    KickAssemblerLexer.HASHDEFINE, KickAssemblerLexer.DEFINED_TOKEN, KickAssemblerLexer.HD_NEWLINE,
                    KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                        KickAssemblerLexer.LDY, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHENDIF, KickAssemblerLexer.EOL,
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
                KickAssemblerLexer.HASHDEFINE, KickAssemblerLexer.DEFINED_TOKEN, KickAssemblerLexer.HD_NEWLINE,
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                KickAssemblerLexer.HASHENDIF,
                KickAssemblerLexer.Eof
            );

            Assert.That(actual.GetTokenTypes(), Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSimpleIfWithInsideUndefinedCondition_ReturnsFilteredTokens()
        {
            const string input = """
                                 #undef DEFINED
                                 #if DEFINED
                                    lda #5
                                 #endif
                                 """;
            
            var actual  = GetTokens(input);

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHUNDEF, KickAssemblerLexer.UNDEFINED_TOKEN, KickAssemblerLexer.HU_NEWLINE,
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN,
                    //KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
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
        public void WhenNestedIfWithUndefinedCondition_ReturnsFilteredTokens()
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
        [Test]
        public void WhenNestedIfWithDefinedCondition_ReturnsFullTokens()
        {
            const string input = """
                                 #if DEFINED
                                    #if UNDEFINED
                                        lda #5
                                    #endif
                                 #endif
                                 """;
            
            var actual  = GetTokens(input, "DEFINED", "UNDEFINED");

            var expected = GetTokenTypes(
                KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHIF, KickAssemblerLexer.IF_TOKEN, KickAssemblerLexer.EOL,
                        KickAssemblerLexer.LDA, KickAssemblerLexer.HASH, KickAssemblerLexer.DEC_NUMBER, KickAssemblerLexer.EOL,
                    KickAssemblerLexer.HASHENDIF, KickAssemblerLexer.EOL,
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