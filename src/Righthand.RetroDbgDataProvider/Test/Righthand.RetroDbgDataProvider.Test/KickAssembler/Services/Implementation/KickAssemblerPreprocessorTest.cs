using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Range = Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation.Range;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

public partial class KickAssemblerPreprocessorTest: BaseTest<KickAssemblerPreprocessor>
{
    CommonTokenStream GetTokenStream(string content)
    {
        var input = new AntlrInputStream(content);
        var lexer = new KickAssemblerLexer(input);
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return stream;
    }
    [TestFixture]
    public partial class ExtractUndefinedRanges : KickAssemblerPreprocessorTest
    {

        [GeneratedRegex("(?<Marker>{(?<End>/)?(?<Number>\\d+)})")]
        private partial Regex Markers();

        /// <summary>
        /// Extracts ranges from content and stores their position.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        (CommonTokenStream Tokens, ImmutableArray<Range> Ranges) GetTokensAndRanges(string content)
        {
            ImmutableArray<(int Start, int End)> textRanges = ImmutableArray<(int Start, int End)>.Empty;
            int i = 0;
            while (true)
            {
                var matches = Markers().Matches(content);
                var start = matches
                    .SingleOrDefault(m => m.Groups["Number"].Value == i.ToString() && m.Groups["End"].Value == "");
                if (start is not null)
                {
                    int startRange = start.Index;
                    int startMarkerLength = 3 + (int)Math.Floor(Math.Log10(3));
                    content = content[..startRange] + content[(startRange + startMarkerLength)..];
                    matches = Markers().Matches(content);
                    var end = matches
                        .SingleOrDefault(m => m.Groups["Number"].Value == i.ToString() && m.Groups["End"].Value == "/");
                    if (end is not null)
                    {
                        int endRange = end.Index;
                        int endMarkerLength = startMarkerLength + 1;
                        textRanges = textRanges.Add((startRange, endRange));
                        content = content[..endRange] + content[(endRange + endMarkerLength)..];
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            var tokenStream = GetTokenStream(content);
            var tokens = tokenStream.GetTokens();
            var ranges = textRanges.Select(r => 
                new Range(tokens.First(t => t.StartIndex >= r.Start), tokens.First(t => t.StartIndex >= r.End))).ToImmutableArray();
            
            return (tokenStream, ranges);
        }

        ImmutableArray<IToken> GetTokens(string content)
        {
            var stream = GetTokenStream(content);
            return [..stream.GetTokens()];
        }

        ImmutableArray<int> GetTokenTypes(string content)
        {
            return [..GetTokens(content).Select(t => t.Type)];
        }
        ImmutableArray<int> GetTokenTypes(CommonTokenStream content)
        {
            return [..content.GetTokens().Select(t => t.Type)];
        }

        FrozenSet<string> GetDefines(params string[] defines)
        {
            return defines.ToFrozenSet();
        }
        [Test]
        public void GivenEmptyStream_NoUndefinedRangesAreReturned()
        {
            var source = GetTokenStream("");
            
            var actual = Target.ExtractUndefinedRanges(source, FrozenSet<string>.Empty);

            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void GivenSimpleContent_NoUndefinedRangesAreReturned()
        {
            var source = GetTokenStream("""
                                        lda #5
                                        """);
            var actual = Target.ExtractUndefinedRanges(source, FrozenSet<string>.Empty);

            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void GivenUndefinedConditionalContent_UndefinedRangeIsReturned()
        {
            var source = GetTokenStream("""
                                        #if NOTDEFINED
                                            lda #5
                                        #endif
                                        """);
            var actual = Target.ExtractUndefinedRanges(source, FrozenSet<string>.Empty);

            var expected = source.WithRanges((3, 7));
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void GivenElseConditionalContent_AndIfIsDefined_UndefinedElseRangeIsReturned()
        {
            string content = """
                             #if DEFINED
                                 lda #5
                             #else{0}
                                 lda #6{/0}
                             #endif
                             """;
            var (source, expected) = GetTokensAndRanges(content);
            var actual = Target.ExtractUndefinedRanges(source, GetDefines("DEFINED"));

            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void GivenElseConditionalContent_AndIfIsUndefined_UndefinedElseRangeIsReturned()
        {
            string content = """
                             #if DEFINED{0}
                                 lda #5{/0}
                             #else
                                 lda #6
                             #endif
                             """;
            var (source, expected) = GetTokensAndRanges(content);
            
            var actual = Target.ExtractUndefinedRanges(source, GetDefines());

            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void GivenElifConditionalContent_AndIfIsDefined_UndefinedElseRangeIsReturned()
        {
            string content = """
                             #if UNDEFINED{0}
                                 lda #5{/0}
                             #elif DEFINED
                                lda #7
                             #else{1}
                                 lda #6{/1}
                             #endif
                             """;
            var (source, expected) = GetTokensAndRanges(content);
            var actual = Target.ExtractUndefinedRanges(source, GetDefines("DEFINED"));

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void GivenDefinedConditionalContent_NoUndefinedRangesAreReturned()
        {
            var source = GetTokenStream("""
                                        #if DEFINED
                                            lda #5
                                        #endif
                                        """);
            var actual = Target.ExtractUndefinedRanges(source,  GetDefines("DEFINED"));
            
            Assert.That(actual, Is.Empty);
        }
        [Test]
        public void GivenNestedDefinedConditionalContent_NoUndefinedRangesAreReturned()
        {
            var source = GetTokenStream("""
                                        #if DEFINED
                                            #if NESTED
                                                lda #5
                                            #endif
                                        #endif
                                        """);
            var actual = Target.ExtractUndefinedRanges(source,  GetDefines("DEFINED", "NESTED"));
            
            Assert.That(actual, Is.Empty);
        }
    }

    [TestFixture]
    public class FilterUndefined : KickAssemblerPreprocessorTest
    {
        [Test]
        public void WhenNoUndefinedRanges_TokensAreNotFiltered()
        {
            var source = GetTokenStream("""
                                        lda #5
                                        """);
            
            Target.FilterUndefined(source, FrozenSet<string>.Empty);
            var actual = source.GetTokens().Select(t => t.Type).ToImmutableArray();
            
            var expected =  GetTokenStream("""
                                           lda #5
                                           """)
                .GetTokens().Select(t => t.Type).ToImmutableArray();
            
            Assert.That(actual, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSingleUndefinedRange_TokensAreFiltered()
        {
            var source = GetTokenStream("""
                                        #if DEFINED
                                            lda #5
                                        #endif
                                        """);
            
            var actual = Target.FilterUndefined(source, FrozenSet<string>.Empty)
                .GetTokens().Select(t => t.Type).ToImmutableArray();
            
            var expected =  GetTokenStream("""
                                           #if DEFINED
                                           #endif
                                           """)
                .GetTokens().Select(t => t.Type).ToImmutableArray();
            
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }

    [TestFixture]
    public class RemoveTokenRanges : KickAssemblerPreprocessorTest
    {
        [Test]
        public void GivenRange_FiltersTokensOut()
        {
            var source = GetTokenStream("""
                                        #if DEFINED
                                            lda #5
                                        #endif
                                        """);

            var tokens = source.GetTokens();
            var actual = Target.RemoveTokenRanges(source, source.WithRanges((3, 7)))
                .GetTokens().Select(t => t.Type).ToImmutableArray();
            
            var expected = GetTokenStream("""
                                          #if DEFINED
                                          #endif
                                          """)
                .GetTokens().Select(t => t.Type).ToImmutableArray();

            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}

internal static class CommonTokenStreamExtensions
{
    private static Range WithRange(this CommonTokenStream tokens, int start, int end)
    {
        Debug.Assert(start <= end);
        return new Range(tokens.Get(start), tokens.Get(end));
    }

    internal static ImmutableArray<string?> GetTexts(this ImmutableArray<Range> source, string content)
    {
       return [..source.Select(r => content[r.From.StartIndex..r.To.StopIndex])];
    }
    internal static ImmutableArray<IList<IToken>> AsTokenLists(this ImmutableArray<Range> source, CommonTokenStream tokens)
    {
        var allTokens = tokens.GetTokens()!;
        return [..source.Select(r => allTokens.Skip(r.From.TokenIndex).Take(r.To.TokenIndex-r.From.TokenIndex).ToImmutableList())];
    }
    internal static ImmutableArray<Range> WithRanges(this CommonTokenStream tokens, params (int Start, int End)[] ranges)
    {
        return [..ranges.Select(r => tokens.WithRange(r.Start, r.End))];
    }
}