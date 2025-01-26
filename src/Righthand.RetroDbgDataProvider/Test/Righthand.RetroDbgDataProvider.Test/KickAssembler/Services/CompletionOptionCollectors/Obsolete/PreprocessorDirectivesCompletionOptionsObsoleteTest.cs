using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors.PreprocessorDirectivesCompletionOptionsObsolete;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class PreprocessorDirectivesCompletionOptionsObsoleteTest
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
    public class GetPreprocessorDirectiveSuggestion : PreprocessorDirectivesCompletionOptionsObsoleteTest
    {
        public record struct TestItem(string Text, LineMeta Expected);

        internal static IEnumerable<TestItem> GetTestItems()
        {
            yield return new TestItem("#import \"|", new(PositionType.Text, "#import", "", "", 0, false));
            yield return new TestItem("#import \"|\"", new(PositionType.Text, "#import", "", "", 0, true));
            yield return new TestItem("#import \"a|\"", new(PositionType.Text, "#import", "a", "a", 1, true));
            yield return new TestItem("#import |", new(PositionType.Expression, "#import", "", "", 0, false));
            yield return new TestItem("#import a|b", new(PositionType.Expression, "#import", "a", "ab", 2, false));
            yield return new TestItem("#import a+|b", new(PositionType.Expression, "#import", "", "b", 1, false));
            yield return new TestItem("#import |a+b", new(PositionType.Expression, "#import", "", "a", 1, false));
        }

        
        [TestCase("#|")]
        [TestCase(" #|")]
        [TestCase("#import |\"\"")]
        [TestCase("#import |\"")]
        public void GivenLine_ReturnsNull(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual, Is.Null);
        }

        [TestCaseSource(nameof(GetTestItems))]
        public void GivenLine_ReturnsCorrectPositionType(TestItem td)
        {
            var (replaced, cursor) = td.Text.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual?.PositionType, Is.EqualTo(td.Expected.PositionType));
        }
        [TestCaseSource(nameof(GetTestItems))]
        public void GivenLine_ReturnsCorrectRoot(TestItem td)
        {
            var (replaced, cursor) = td.Text.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual?.Root, Is.EqualTo(td.Expected.Root));
        }
        [TestCaseSource(nameof(GetTestItems))]
        public void GivenLine_ReturnsCorrectCurrentValue(TestItem td)
        {
            var (replaced, cursor) = td.Text.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual?.CurrentValue, Is.EqualTo(td.Expected.CurrentValue));
        }
        [TestCaseSource(nameof(GetTestItems))]
        public void GivenLine_ReturnsCorrectReplacementLength(TestItem td)
        {
            var (replaced, cursor) = td.Text.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual?.ReplacementLength, Is.EqualTo(td.Expected.ReplacementLength));
        }
        [TestCaseSource(nameof(GetTestItems))]
        public void GivenLine_ReturnsCorrectHasEndDelimiter(TestItem td)
        {
            var (replaced, cursor) = td.Text.ExtractCaret();
            var tokens = GetAllTokens(replaced);
            var actual = GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);

            Assert.That(actual?.HasEndDelimiter, Is.EqualTo(td.Expected.HasEndDelimiter));
        }
    }
}