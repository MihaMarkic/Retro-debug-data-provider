using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class PreprocessorExpressionCompletionOptionsTest
{
    private static ImmutableArray<IToken> GetAllTokens(string text)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens().Where(t => t.Channel == 0);
        return [..tokens];
    }

    [TestFixture]
    public class GetMetaInformation : PreprocessorExpressionCompletionOptionsTest
    {
        [TestCase("#if ALFA|+BETA", ExpectedResult = "ALFA")]
        [TestCase("#if ALFA+|BETA", ExpectedResult = "")]
        [TestCase("#if ALFA+BE|TA", ExpectedResult = "BE")]
        [TestCase("#if ALFA+BETA|", ExpectedResult = "BETA")]
        public string? GivenTestCase_ReturnsRoot(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var tokens = GetAllTokens(replaced);
        
            return PreprocessorExpressionCompletionOptions.GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor)?.Root;
        }
    }

    [TestFixture]
    public class GetMetaFromExpression : PreprocessorExpressionCompletionOptionsTest
    {
        [TestCase("A|LFA", ExpectedResult = "A")]
        [TestCase("|ALFA", ExpectedResult = "")]
        [TestCase("ALFA|", ExpectedResult = "ALFA")]
        public string? GivenTestCase_GetsCorrectRoot(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            return PreprocessorExpressionCompletionOptions.GetMetaFromExpression(replaced, cursor).Root;
        }
    }
        
}