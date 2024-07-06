using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Test.Models.Program;

public class TextRangeTest: BaseTest<TextRange>
{
    [TestFixture]
    public class Contains: TextRangeTest
    {
        [TestCase(0, 0, 0, 5, 0, 2, ExpectedResult = true)]
        [TestCase(0, 0, 0, 5, 0, 6, ExpectedResult = false)]
        [TestCase(0, 0, 1, 5, 0, 8, ExpectedResult = true)]
        public bool GivenTestCases_ReturnsExpected(int rowStart, int colStart, int rowEnd, int colEnd,
            int row, int col)
        {
            return new TextRange(new TextCursor(rowStart, colStart), new TextCursor(rowEnd, colEnd))
                .Contains(new TextCursor(row, col));
        }
    }
}