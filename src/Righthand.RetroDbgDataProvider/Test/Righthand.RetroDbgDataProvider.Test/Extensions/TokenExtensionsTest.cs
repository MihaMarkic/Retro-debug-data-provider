using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Test.Mocks;

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
}