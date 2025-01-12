using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class DirectiveCompletionOptionsTest
{
    [TestFixture]
    public class GetMetaInformation : DirectiveCompletionOptionsTest
    {
        [TestCase(".import c64 \"|")]
        [TestCase(".import c64 \"xx/p|")]
        [TestCase("label: .import c64 \"xx/p|")]
        [TestCase("\"\".import c64 \"|")]
        [TestCase("\"xxx\".import c64 \"|")]
        public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNonNullResult(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);

            Assert.That(actual, Is.Not.Null);
        }
        [TestCase(".import c64 [\"|")]
        [TestCase(".import c64 x \"xx/p|")]
        [TestCase("label: .print c64 \"|")]
        public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNullResult(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);

            Assert.That(actual, Is.Null);
        }
        
        [TestCase(".import c64 \"|", ExpectedResult = "")]
        [TestCase(".import c64 \"xx/p|", ExpectedResult = "xx/p")]
        [TestCase("label: .import c64 \"xx/p|", ExpectedResult = "xx/p")]
        public string? GivenSampleInputWithOnlyPrefix_ReturnsCorrectRoot(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);
            return actual?.Root;
        }
        [TestCase(".import c64 \"x|x/p", ExpectedResult = "x")]
        [TestCase("label: .import c64 \"xx/|p", ExpectedResult = "xx/")]
        [TestCase(".import c64 \"test2|\"", ExpectedResult = "test2")]
        [TestCase("label: .import c6|4 \"xx/p", ExpectedResult = "c6")]
        [TestCase(".import c|64 \"test2\"", ExpectedResult = "c")]
        [TestCase(".import |c64 \"test2\"", ExpectedResult = "")]
        [TestCase(".import |", ExpectedResult = "")]
        public string? GivenSampleInputWithCursorInBetween_ReturnsCorrectRoot(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);
            return actual?.Root;
        }
        [TestCase("label: .import c6|4 \"xx/p", ExpectedResult = 3)]
        [TestCase(".import c|64 \"test2\"", ExpectedResult = 3)]
        [TestCase(".import |c64 \"test2\"", ExpectedResult = 3)]
        [TestCase(".import |c6 \"test2\"", ExpectedResult = 2)]
        [TestCase(".import c|6 \"test2\"", ExpectedResult = 2)]
        [TestCase(".import |", ExpectedResult = 0)]
        public int? GivenSampleInputWithCursorInBetween_ReturnsCorrectReplacementLength(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);
            return actual?.ReplacementLength;
        }
        [TestCase(".import c64 \"x|x/p", ExpectedResult = DirectiveCompletionOptions.PositionType.Value)]
        [TestCase("label: .import c64 \"xx/|p", ExpectedResult = DirectiveCompletionOptions.PositionType.Value)]
        [TestCase(".import c64 \"test2|\"", ExpectedResult = DirectiveCompletionOptions.PositionType.Value)]
        [TestCase(".import c64| \"xx/p", ExpectedResult = DirectiveCompletionOptions.PositionType.Type)]
        [TestCase("label: .import c6|4 \"xx/p", ExpectedResult = DirectiveCompletionOptions.PositionType.Type)]
        [TestCase(".import c|64 \"test2\"", ExpectedResult = DirectiveCompletionOptions.PositionType.Type)]
        [TestCase(".import |c64 \"test2\"", ExpectedResult = DirectiveCompletionOptions.PositionType.Type)]
        [TestCase(".import |", ExpectedResult = DirectiveCompletionOptions.PositionType.Type)]
        public DirectiveCompletionOptions.PositionType? GivenSampleInputWithCursorInBetween_ReturnsCorrectPositionType(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);
            return actual?.PositionType;
        }
        [TestCase(".import c|6 \"test2\"", ExpectedResult = false)]
        [TestCase(".import c64 \"test2|\"", ExpectedResult = true)]
        [TestCase(".import c64 \"test2|", ExpectedResult = false)]
        [TestCase(".import |", ExpectedResult = false)]
        public bool? GivenSampleInputWithCursorInBetween_ReturnsCorrectHasEndDelimiter(string line)
        {
            var (replaced, cursor) = line.ExtractCaret();
            var actual =
                DirectiveCompletionOptions.GetMetaInformation(replaced, 0, replaced.Length, cursor);
            return actual?.HasEndDelimiter;
        }
    }

    // [TestFixture]
    // public class GetStatus : DirectiveCompletionOptionsTest
    // {
    //     private static (ImmutableArray<IToken> Tokens, int Caret) GetAllTokens(string text)
    //     {
    //         var (corrected, caret) = text.ExtractCaret();
    //         var input = new AntlrInputStream(corrected);
    //         var lexer = new KickAssemblerLexer(input);
    //         var stream = new BufferedTokenStream(lexer);
    //         stream.Fill();
    //         var tokens = stream.GetTokens().Where(t => t.Channel == 0);
    //         return ([..tokens], caret);
    //     }
    //     
    //     [TestCase(".import c64 \"|")]
    //     [TestCase(".import c64 \"xx/p|")]
    //     [TestCase("label: .import c64 \"xx/p|")]
    //     [TestCase("\"\".import c64 \"|")]
    //     [TestCase("\"xxx\".import c64 \"|")]
    //     public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNonNullResult(string line)
    //     {
    //         var (allTokens, cursor) = GetAllTokens(line);
    //         var actual = DirectiveCompletionOptions.GetStatus(allTokens.AsSpan(), line, cursor);
    //
    //         Assert.That(actual, Is.Not.Null);
    //     }
    // }
}