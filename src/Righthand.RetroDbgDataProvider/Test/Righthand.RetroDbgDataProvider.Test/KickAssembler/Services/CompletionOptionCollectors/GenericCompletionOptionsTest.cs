using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class GenericCompletionOptionsTest: CompletionOptionTestBase
{
    [TestFixture]
    public class GetOption : GenericCompletionOptionsTest
    {
        [TestCase("|", 0, ExpectedResult = "")]
        [TestCase("#impo|rt", 0, ExpectedResult = "#impo")]
        [TestCase(".encod|ing", 0, ExpectedResult = ".encod")]
        public string? GivenTestCase_ReturnsCorrectRoot(string input, int start)
        {
            var tc = CreateCase(input, start);

            var actual = GenericCompletionOptions.GetOption(tc.Tokens.AsSpan(), tc.Content, tc.Start, tc.End, tc.Column, NoOpContext);

            return actual?.RootText;
        }
    }
}