using Antlr4.Runtime;
using NUnit.Framework;

namespace Righthand.RetroDbgDataProvider.Test.Extensions;

public class TokenExtensionsTest
{
    [TestFixture]
    public class TextUpToColumn : TokenExtensionsTest
    {
        [TestCase("Test2", 0, 2, ExpectedResult = "Te")]
        [TestCase("Test2", 10, 12, ExpectedResult = "Te")]
        public string GivenSampleCase_ReturnsCorrectText(string text, int startIndex, int absoluteColumnIndex)
        {
            var token = new MockToken
            {
                Text = text,
                StartIndex = startIndex,
                StopIndex = startIndex + text.Length - 1,
            };
            
            return token.TextUpToColumn(absoluteColumnIndex);
        }
    }
    
    public class MockToken: IToken
    {
        public required string Text { get; init; }
        public int Type { get; } = 0;
        public int Line { get; } = 0;
        public int Column { get; } = 0;
        public int Channel { get; } = 0;
        public int TokenIndex { get; } = 0;
        public required int StartIndex { get; init; }
        public required int StopIndex { get; init; }
        public ITokenSource TokenSource { get; } = null!;
        public ICharStream InputStream { get; } = null!;
    }
}