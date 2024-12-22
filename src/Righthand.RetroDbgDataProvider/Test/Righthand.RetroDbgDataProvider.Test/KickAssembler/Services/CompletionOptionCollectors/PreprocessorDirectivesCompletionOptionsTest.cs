using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class PreprocessorDirectivesCompletionOptionsTest
{
    [TestFixture]
    public class GetPreprocessorDirectiveSuggestion : PreprocessorDirectivesCompletionOptionsTest
    {
        [TestCase("#", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        [TestCase(" #", 1, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        public bool GivenLine_ReturnsIsMatch(string line, int column, TextChangeTrigger trigger)
        {
            return PreprocessorDirectivesCompletionOptions.GetPreprocessorDirectiveSuggestion(line, trigger, column).IsMatch;
        }

        [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        public string GivenLine_ReturnsRoot(string line, int column, TextChangeTrigger trigger)
        {
            return PreprocessorDirectivesCompletionOptions.GetPreprocessorDirectiveSuggestion(line, trigger, column).Root;
        }

        [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "import")]
        public string GivenLine_ReturnsReplaceableText(string line, int column, TextChangeTrigger trigger)
        {
            return PreprocessorDirectivesCompletionOptions.GetPreprocessorDirectiveSuggestion(line, trigger, column)
                .ReplaceableText;
        }
    }
}