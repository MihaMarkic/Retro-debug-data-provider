using System.Text;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.Services.Implementation;

public class WindowsDependentTest: BaseTest<WindowsDependent>
{
    [TestFixture]
    public class ReadAllTextAndAdjustLineEndingsAsync : WindowsDependentTest
    {
        private Stream OpenStream(string text)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        [TestCase("", ExpectedResult = "")]
        [TestCase("\r\n", ExpectedResult = "\r\n")]
        [TestCase("\n", ExpectedResult = "\r\n")]
        [TestCase("\n\r\n", ExpectedResult = "\r\n\r\n")]
        [TestCase("one\ntwo", ExpectedResult = "one\r\ntwo")]
        public async Task<string> GivenSampleInputText_ReturnsCorrectLineEndings(string input)
        {
            await using var stream = OpenStream(input);

            return await Target.ReadAllTextAndAdjustLineEndingsAsync(stream);
        }
    }
}