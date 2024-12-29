using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using GetOptionTestCase =
    (System.Collections.Immutable.ImmutableArray<Antlr4.Runtime.IToken> Tokens, string Content, int Start, int End, int Column,
    Righthand.RetroDbgDataProvider.Models.Parsing.CompletionOption? ExpectedResult);

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class BodyArrayCompletionOptionsTest
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

    private static GetOptionTestCase CreateCase<T>(string text, int start, int end, T? expectedResult)
        where T: CompletionOption
    {
        Debug.Assert(text.Count(c => c == '|') == 1, "Exactly one cursor | is allowed within text");
        int cursor = Math.Max(text.IndexOf('|') - 1, 0);
        text = text.Replace("|", "");
        return (GetAllTokens(text), text, start, end, cursor, expectedResult);
    }

    [TestFixture]
    public class GetOption : BodyArrayCompletionOptionsTest
    {
        private static IEnumerable<GetOptionTestCase> GetTestCasesForNameWithExpectedResult()
        {
            var defaultSuggestions = ArrayProperties.GetNames(".DISK_TYPE")
                .Select(s => new Suggestion(SuggestionOrigin.PropertyName, s))
                .ToFrozenSet();
            yield return CreateCase(".disk { [hide|", 0, 0, new ArrayPropertyNameCompletionOption( "hide",  0, []));
            yield return CreateCase(".disk { [|", 0, 0, new ArrayPropertyNameCompletionOption( "",  0, defaultSuggestions));
            yield return CreateCase(".disk { [hide|x", 0, 0, new ArrayPropertyNameCompletionOption( "hide",  1, []));
            yield return CreateCase(".disk { [name=1,hide|", 0, 0, new ArrayPropertyNameCompletionOption( "hide",  0, []));
        }

        private static IEnumerable<GetOptionTestCase> GetTestCasesForValueWithExpectedResult()
        {
            ArrayProperties.GetProperty(".DISK_FILE", "hide", out var property);

            var defaultSuggestions = ArrayPropertyValues.BoolValues
                .Select(s => new Suggestion(SuggestionOrigin.PropertyName, s))
                .ToFrozenSet();
            yield return CreateCase(".disk { [hide=\"|x", 0, 0, new ArrayPropertyValueCompletionOption("", 1, false, property, defaultSuggestions));
            // yield return CreateCase("""
            //                         .disk {
            //                             [hide=|
            //                         """, 0, 0, new ArrayPropertyNameCompletionOption( "",  0, null));
            // yield return CreateCase("""
            //                         .disk {
            //                             [shema = 4, tubo],
            //                             [hide=|
            //                         """, 0, 0, new ArrayPropertyNameCompletionOption( "",  0, null));
        }

        private static IEnumerable<GetOptionTestCase> GetTestCasesForNameWithoutExpectedResult()
        {
            yield return CreateCase<ArrayPropertyNameCompletionOption>("|", 0, 0, null);
            yield return CreateCase<ArrayPropertyNameCompletionOption>("[hide=|", 0, 0, null);
            yield return CreateCase<ArrayPropertyNameCompletionOption>("""
                                    [t=|
                                    """, 0, 0, null);
        }

        [TestCaseSource(nameof(GetTestCasesForNameWithoutExpectedResult))]
        public void GivenSample_ReturnsNullExpectedResult(GetOptionTestCase td)
        {
            var actual = BodyArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column);

            Assert.That(actual, Is.Null);
        }
        [TestCaseSource(nameof(GetTestCasesForNameWithExpectedResult))]
        public void GivenSampleForName_ReturnsExpectedResult(GetOptionTestCase td)
        {
            var actual = BodyArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult).Using(ArrayPropertyNameCompletionOptionComparer.Default));
        }
        [TestCaseSource(nameof(GetTestCasesForValueWithExpectedResult))]
        public void GivenSampleForValue_ReturnsExpectedResult(GetOptionTestCase td)
        {
            var actual = BodyArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult).Using(ArrayPropertyNameCompletionOptionComparer.Default));
        }
    }
}