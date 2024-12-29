using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.Models.Parsing.CompletionOptionType;
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

    private static GetOptionTestCase CreateCase(string text, int start, int end, CompletionOption? expectedResult)
    {
        Debug.Assert(text.Count(c => c == '|') == 1, "Exactly one cursor | is allowed within text");
        int cursor = Math.Max(text.IndexOf('|') - 1, 0);
        text = text.Replace("|", "");
        return (GetAllTokens(text), text, start, end, cursor, expectedResult);
    }

    [TestFixture]
    public class GetOption : BodyArrayCompletionOptionsTest
    {
        private static IEnumerable<GetOptionTestCase> GetTestCasesWithExpectedResult()
        {
            yield return CreateCase(".disk { [t|", 0, 0, new CompletionOption(ArrayPropertyName, "t", false, 0, [], ".DISK_FILE"));
            yield return CreateCase(".disk { [|", 0, 0, new CompletionOption(ArrayPropertyName, "", false, 0, [], ".DISK_FILE"));
            yield return CreateCase(".disk { [t|x", 0, 0, new CompletionOption(ArrayPropertyName, "t", false, 1, [], ".DISK_FILE"));
            yield return CreateCase(".disk { [t=\"|x", 0, 0, new CompletionOption(ArrayPropertyValue, "", false, 1, [], ".DISK_FILE", "t"));
            yield return CreateCase(".disk { [a=1,t|", 0, 0, new CompletionOption(ArrayPropertyName, "t", false, 0, ["a"], ".DISK_FILE"));
            yield return CreateCase("""
                                    .disk {
                                        [t=|
                                    """, 0, 0, new CompletionOption(ArrayPropertyValue, "", false, 0, [], ".DISK_FILE", "t"));
            yield return CreateCase("""
                                    .disk {
                                        [shema = 4, tubo],
                                        [t=|
                                    """, 0, 0, new CompletionOption(ArrayPropertyValue, "", false, 0, [], ".DISK_FILE", "t"));
        }
        private static IEnumerable<GetOptionTestCase> GetTestCasesWithoutExpectedResult()
        {
            yield return CreateCase("|", 0, 0, null);
            yield return CreateCase("[t=|", 0, 0, null);
            yield return CreateCase("""
                                    [t=|
                                    """, 0, 0, null);
        }

        [TestCaseSource(nameof(GetTestCasesWithoutExpectedResult))]
        public void GivenSample_ReturnsNullExpectedResult(GetOptionTestCase td)
        {
            var actual = BodyArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column);

            Assert.That(actual, Is.Null);
        }
        [TestCaseSource(nameof(GetTestCasesWithExpectedResult))]
        public void GivenSample_ReturnsExpectedResult(GetOptionTestCase td)
        {
            var actual = BodyArrayCompletionOptions.GetOption(td.Tokens.AsSpan(), td.Content, td.Start, td.End, td.Column);

            Assert.That(actual, Is.EqualTo(td.ExpectedResult).Using(CompletionOptionComparer.Default));
        }
    }
}