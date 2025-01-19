using System.Collections.Immutable;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class PreprocessorDirectivesCompletionOptionsTest
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
    public class GetPreprocessorDirectiveSuggestion : PreprocessorDirectivesCompletionOptionsTest
    {
        // [TestCase("#|", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        // [TestCase(" #|", 1, TextChangeTrigger.CharacterTyped, ExpectedResult = true)]
        // public bool GivenLine_ReturnsIsMatch(string line, int column, TextChangeTrigger trigger)
        // {
        //     var (replaced, cursor) = line.ExtractCaret();
        //     var tokens = GetAllTokens(replaced);
        //     var actual = PreprocessorDirectivesCompletionOptions.GetMetaInformation(tokens.AsSpan(), replaced, 0, replaced.Length, cursor);
        //
        //     return actual.IsMatch;
        //
        // }

        // [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        // [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        // [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        // public string GivenLine_ReturnsRoot(string line, int column, TextChangeTrigger trigger)
        // {
        //     return PreprocessorDirectivesCompletionOptions.GetMetaInformation(line, trigger, column).Root;
        // }
        //
        // [TestCase("#im", 0, TextChangeTrigger.CharacterTyped, ExpectedResult = "")]
        // [TestCase("#im", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "im")]
        // [TestCase("#import", 2, TextChangeTrigger.CompletionRequested, ExpectedResult = "import")]
        // public string GivenLine_ReturnsReplaceableText(string line, int column, TextChangeTrigger trigger)
        // {
        //     return PreprocessorDirectivesCompletionOptions.GetMetaInformation(line, trigger, column)
        //         .ReplaceableText;
        // }
    }
}