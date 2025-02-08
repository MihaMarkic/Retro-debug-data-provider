using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class CompletionOptionCollectorsCommonTest
{
    [TestFixture]
    public class TrimWhitespaces : CompletionOptionCollectorsCommonTest
    {
        [TestCaseSource(typeof(GivenSampleTrimsProperlySource))]
        public void GivenSample_TrimsProperly(ImmutableArray<IToken> data, ImmutableArray<IToken> expected)
        {
            var actual = CompletionOptionCollectorsCommon.TrimWhitespaces(data.AsSpan());

            Assert.That(actual.ToImmutableArray(), Is.EquivalentTo(expected));
        }

        private class GivenSampleTrimsProperlySource : IEnumerable
        {
            private static ImmutableArray<IToken> GetArray(
                params (int TokenType, int Channel)[] tokens) =>
                [..tokens.Select(t => new TrimWhitespacesToken(t.TokenType, t.Channel))];

            public IEnumerator GetEnumerator()
            {
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.STRING, 0)),
                    GetArray((KickAssemblerLexer.STRING, 0)));
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.WS, 0)),
                    GetArray());
                yield return new TestCaseData(
                    GetArray((KickAssemblerLexer.WS, 0), (KickAssemblerLexer.STRING, 0), (KickAssemblerLexer.EOL, 1)),
                    GetArray((KickAssemblerLexer.STRING, 0)));
            }
        }

        [TestFixture]
        public class GetSuggestionTextInDoubleQuotes : CompletionOptionCollectorsCommonTest
        {
            [TestCase("", 0, ExpectedResult = "")]
            [TestCase("xys", 0, ExpectedResult = "")]
            [TestCase("xys", 1, ExpectedResult = "x")]
            [TestCase("xys", 2, ExpectedResult = "xy")]
            [TestCase("xys", 3, ExpectedResult = "xys")]
            [TestCase("xys\"", 3, ExpectedResult = "xys")]
            public string GivenSample_ReturnsCorrectRoot(string input, int caret)
            {
                return CompletionOptionCollectorsCommon.GetSuggestionTextInDoubleQuotes(input, caret).RootText;
            }

            [TestCase("", 0, ExpectedResult = 0)]
            [TestCase("xys", 0, ExpectedResult = 3)]
            [TestCase("xys", 2, ExpectedResult = 3)]
            [TestCase("xys", 3, ExpectedResult = 3)]
            [TestCase("xys\"", 3, ExpectedResult = 3)]
            public int GivenSample_ReturnsCorrectLength(string input, int caret)
            {
                return CompletionOptionCollectorsCommon.GetSuggestionTextInDoubleQuotes(input, caret).Length;
            }

            [TestCase("xys", 3, ExpectedResult = false)]
            [TestCase("xys\"", 3, ExpectedResult = true)]
            [TestCase("xys \t\"", 3, ExpectedResult = true)]
            public bool GivenSample_ReturnsCorrectEndsWithDoubleQuote(string input, int caret)
            {
                return CompletionOptionCollectorsCommon.GetSuggestionTextInDoubleQuotes(input, caret).EndsWithDoubleQuote;
            }
        }

        [TestFixture]
        public class CollectFileSystemSuggestions : CompletionOptionCollectorsCommonTest
        {
            [Test]
            public void WhenSourceFileIsInRootAndSubdirectoryIsFound_ReturnsFullRelativePath()
            {
                var directory = new Dictionary<ProjectFileKey, FrozenSet<string>>
                {
                    { new (ProjectFileOrigin.Project, "project_path"), ["xxx/ddd"] }
                }.ToFrozenDictionary();
                var projectServices = Substitute.For<IProjectServices>();
                projectServices
                    .GetMatchingFiles("", "", [], [])
                    .ReturnsForAnyArgs(FrozenDictionary<ProjectFileKey, FrozenSet<string>>.Empty);
                projectServices
                    .GetMatchingDirectories("", "")
                    .ReturnsForAnyArgs(directory);
                
                var actual = CompletionOptionCollectorsCommon.CollectFileSystemSuggestions("", "xxx/", [], [], projectServices);

                var expected = new DirectorySuggestion("xxx/ddd", ProjectFileOrigin.Project, "project_path");
                
                Assert.That(actual.Single(), Is.EqualTo(expected));
            }
        }

        private record TrimWhitespacesToken : IToken
        {
            public string Text => "N/A";
            public int Type { get; }
            public int Line => -1;
            public int Column => -1;
            public int Channel { get; }
            public int TokenIndex => -1;
            public int StartIndex => -1;
            public int StopIndex => -1;
            public ITokenSource TokenSource => null!;
            public ICharStream InputStream => null!;

            public TrimWhitespacesToken(int type, int channel)
            {
                Type = type;
                Channel = channel;
            }
        }
    }
}