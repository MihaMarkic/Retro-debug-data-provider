using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class QuotedCompletionOptionsTest
{
    [TestFixture]
    public class IsCursorWithinNonArray : QuotedCompletionOptionsTest
    {
        [TestCase(".import c64 \"")]
        [TestCase(".import c64 \"xx/p")]
        [TestCase("label: .import c64 \"xx/p")]
        [TestCase("\"\".import c64 \"")]
        [TestCase("\"xxx\".import c64 \"")]
        public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNonNullResult(string line)
        {
            var actual =
                QuotedCompletionOptions.IsCursorWithinNonArray(line, 0, line.Length, line.Length-1);

            Assert.That(actual, Is.Not.Null);
        }
        [TestCase(".import c64 [\"")]
        [TestCase(".import c64 x \"xx/p")]
        [TestCase("label: .print c64 \"")]
        [TestCase("\".import c64 \"")]
        [TestCase("\"xxx\"\".import c64 \"")]
        [TestCase("//\"\".import c64 \"")]
        public void GivenSampleInputThatPutsCursorWithinArray_ReturnsNullResult(string line)
        {
            var actual =
                QuotedCompletionOptions.IsCursorWithinNonArray(line, 0, line.Length, line.Length-1);

            Assert.That(actual, Is.Null);
        }
        
        [TestCase(".import c64 \"", ExpectedResult = "")]
        [TestCase(".import c64 \"xx/p", ExpectedResult = "xx/p")]
        [TestCase("label: .import c64 \"xx/p", ExpectedResult = "xx/p")]
        public string? GivenSampleInputWithOnlyPrefix_ReturnsCorrectRoot(string line)
        {
            var actual =
                QuotedCompletionOptions.IsCursorWithinNonArray(line, 0, line.Length, line.Length-1);
            return actual?.Root;
        }
        [TestCase(".import c64 \"xx/p", 13, ExpectedResult = "x")]
        [TestCase("label: .import c64 \"xx/p", 22, ExpectedResult = "xx/")]
        [TestCase(".import c64 \"test2\"", 17, ExpectedResult = "test2")]
        public string? GivenSampleInputWithCursorInBetween_ReturnsCorrectRoot(string line, int cursor)
        {
            var actual =
                QuotedCompletionOptions.IsCursorWithinNonArray(line, 0, line.Length, cursor);
            return actual?.Root;
        }
    }
}