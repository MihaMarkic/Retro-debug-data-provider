using System.Text;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.Services.Implementation;

public class NonWindowsDependentTest: BaseTest<NonWindowsDependentForTest>
{
    public class ReadAllTextAndAdjustLineEndingsAsync : NonWindowsDependentTest
    {
        private Stream OpenStream(string text)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        [TestCase("", ExpectedResult = "")]
        [TestCase("\r\n", ExpectedResult = "\n")]
        [TestCase("\r\nalpha", ExpectedResult = "\nalpha")]
        [TestCase("one\ntwo", ExpectedResult = "one\ntwo")]
        public async Task<string> GivenSampleInputText_ReturnsCorrectLineEndings(string input)
        {
            await using var stream = OpenStream(input);

            return await Target.ReadAllTextAndAdjustLineEndingsAsync(stream);
        }
    }
}
// ReSharper disable once ClassNeverInstantiated.Global
public class NonWindowsDependentForTest: NonWindowsDependent
{}