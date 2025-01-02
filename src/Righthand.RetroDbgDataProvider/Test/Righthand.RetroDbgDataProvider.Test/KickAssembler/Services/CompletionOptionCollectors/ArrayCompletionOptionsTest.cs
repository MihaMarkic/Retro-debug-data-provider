using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using GetOptionTestCase =
    (System.Collections.Immutable.ImmutableArray<Antlr4.Runtime.IToken> Tokens, string Content, int Start, int End, int Column,
    Righthand.RetroDbgDataProvider.Models.Parsing.CompletionOption? ExpectedResult);

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class ArrayCompletionOptionsTest
{
    private static CompletionOptionContext NoOpContext { get; } = new CompletionOptionContext(
        Substitute.For<IProjectServices>()
    );
    
    private static FrozenSet<T> CreateExpectedResult<T>(string values, Func<string, T> create)
    {
        return values.Split(',')
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(create)
            .ToFrozenSet();
    }
    private static FrozenSet<string> CreateExpectedResultSet(string values)
    {
        return values.Split(',')
            .Where(t => !string.IsNullOrEmpty(t))
            .ToFrozenSet();
    }

    private static ImmutableArray<IToken> GetAllTokens(string text)
    {
        var input = new AntlrInputStream(text);
        var lexer = new KickAssemblerLexer(input);
        var stream = new BufferedTokenStream(lexer);
        stream.Fill();
        var tokens = stream.GetTokens().Where(t => t.Channel == 0);
        return [..tokens];
    }

    private static GetOptionTestCase CreateCase(string text, int start, int end, CompletionOption? expectedResult = null)
    {
        Debug.Assert(text.Count(c => c == '|') == 1, "Exactly one cursor | is allowed within text");
        int cursor = Math.Max(text.IndexOf('|') - 1, 0);
        text = text.Replace("|", "");
        return (GetAllTokens(text), text, start, end, cursor, expectedResult);
    }

    [TestFixture]
    public class GetOption : ArrayCompletionOptionsTest
    {
        private static IEnumerable<GetOptionTestCase> GetTestCasesForNameWithExpectedResult()
        {
            var defaultSuggestions = ArrayProperties.GetNames(".DISK_FILE")
                .Select(s => new StandardSuggestion(SuggestionOrigin.PropertyName, s))
                .Cast<Suggestion>()
                .ToFrozenSet();
            yield return CreateCase(".disk { [hide|", 0, 0, new CompletionOption("hide", 0, "", []));
            yield return CreateCase(".disk { [|", 0, 0, new CompletionOption("", 0, "", defaultSuggestions));
            yield return CreateCase(".disk { [hide|x", 0, 0, new CompletionOption("hide", 1, "", []));
            yield return CreateCase(".disk { [name=1,hide|", 0, 0, new CompletionOption("hide", 0, "", []));
        }
        [TestCaseSource(nameof(GetTestCasesForNameWithExpectedResult))]
        public void GivenSampleForName_ReturnsExpectedResult(GetOptionTestCase td)
        {
            var actual = ArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column, NoOpContext);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }


        private static IEnumerable<GetOptionTestCase> GetTestCasesForValueWithExpectedResult()
        {
            ArrayProperties.GetProperty(".DISK_FILE", "hide", out var property);

            var defaultSuggestions = ArrayPropertyValues.BoolValues
                .Select(s => new StandardSuggestion(SuggestionOrigin.PropertyValue, s))
                .Cast<Suggestion>()
                .ToFrozenSet();
            yield return CreateCase(".disk { [hide=\"|x", 0, 0, new CompletionOption("\"", 2, "", []));
            yield return CreateCase("""
                                    .disk {
                                        [hide=|
                                    """, 0, 0, new CompletionOption( "",  0,"", defaultSuggestions));
            yield return CreateCase("""
                                    .disk {
                                        [shema = 4, tubo],
                                        [hide=|
                                    """, 0, 0, new CompletionOption( "",  0, "", defaultSuggestions));
        }
        [TestCaseSource(nameof(GetTestCasesForValueWithExpectedResult))]
        public void GivenSampleForValue_ReturnsExpectedResult(GetOptionTestCase td)
        {
            var actual = ArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column, NoOpContext);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult));
        }

        private static IEnumerable<GetOptionTestCase> GetTestCasesForNameWithoutExpectedResult()
        {
            yield return CreateCase("|", 0, 0);
            yield return CreateCase("[hide=|", 0, 0);
            yield return CreateCase("""
                                    [t=|
                                    """, 0, 0);
        }

        [TestCaseSource(nameof(GetTestCasesForNameWithoutExpectedResult))]
        public void GivenSample_ReturnsNullExpectedResult(GetOptionTestCase td)
        {
            var actual = ArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column, NoOpContext);

            Assert.That(actual, Is.Null);
        }
    }

    [TestFixture]
    public class CreateSuggestionsForArrayValue : ArrayCompletionOptionsTest
    {
        private static FrozenSet<Suggestion> CreateExpectedResult(string values) =>
            CreateExpectedResult<Suggestion>(values, v => new StandardSuggestion(SuggestionOrigin.PropertyValue, v));

        [TestCase("", "hide", "", "true,false")]
        [TestCase("t", "hide", "", "true")]
        public void GivenTestCaseForBoolValue_ReturnsCorrectSuggestions(string root, string propertyName, string? value, string expectedResult)
        {
            var arrayProperty = new ValuesArrayProperty("hide", ArrayPropertyType.Bool, ArrayPropertyValues.BoolValues.ToFrozenSet());

            var actual = ArrayCompletionOptions.CreateSuggestionsForArrayValue(root, propertyName, value, arrayProperty, NoOpContext)
                .Suggestions;

            var expected = CreateExpectedResult(expectedResult);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("", "type", "", "\"prg\",\"bin\"")]
        [TestCase("\"pr", "type", "", "\"prg\"")]
        [TestCase("pr", "type", "", "")]
        public void GivenTestCaseForQuotedEnumerableValue_ReturnsCorrectSuggestions(string root, string propertyName, string? value,
            string expectedResult)
        {
            var arrayProperty = new ValuesArrayProperty("hide", ArrayPropertyType.QuotedEnumerable,
                new HashSet<string> { "prg", "bin" }.ToFrozenSet());

            var actual = ArrayCompletionOptions.CreateSuggestionsForArrayValue(root, propertyName, value, arrayProperty, NoOpContext)
                .Suggestions;

            var expected = CreateExpectedResult(expectedResult);

            Assert.That(actual, Is.EqualTo(expected));
        }
        [TestCase("", "segments", "", "sx1,sy2")]
        [TestCase("s", "segments", "", "sx1,sy2")]
        [TestCase("sx", "segments", "", "sx1")]
        [TestCase("sx1", "segments", "", "")]
        public void GivenTestCaseForSegments_ReturnsCorrectSuggestions(string root, string propertyName, string? value,
            string expectedResult)
        {

            var projectServices = Substitute.For<IProjectServices>();
            projectServices.CollectSegments().Returns(["sx1", "sy2"]);
            var arrayProperty = new ValuesArrayProperty("segments", ArrayPropertyType.Segments);
            
            var actual = ArrayCompletionOptions
                .CreateSuggestionsForArrayValue(root, propertyName, value, arrayProperty, new CompletionOptionContext(projectServices))
                .Suggestions;

            var expected = CreateExpectedResult(expectedResult);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [TestFixture]
    public class GetArrayValues : ArrayCompletionOptionsTest
    {
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("\"a", "a")]
        [TestCase("\"a\"", "a")]
        [TestCase("\"a, a b", "a,a b")]
        [TestCase("\"alfa.prg", "alfa.prg")]
        [TestCase("\"alfa.prg,", "alfa.prg")]
        [TestCase("\"alfa.prg,a.prg", "alfa.prg,a.prg")]
        [TestCase("\"alfa.prg,a.prg\"", "alfa.prg,a.prg")]

        public void GivenTestCase_ReturnsCorrectSet(string? values, string expectedText)
        {
            var actual = ArrayCompletionOptions.GetArrayValues(values);
            
            var expected = CreateExpectedResultSet(expectedText);
            
            Assert.That(actual.SetEquals(expected), Is.True);
        }
    }
}