using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class ArrayContentExtractorTest
{
    [TestFixture]
    public class Extract : ArrayContentExtractorTest
    {
        public class ExtractSource : IEnumerable
        {
            private ImmutableArray<KeyValuePair<string, string>> CreateExpected(params (string Name, string Value)[] values)
            {
                return [..values.Select(v => new KeyValuePair<string, string>(v.Name, v.Value))];
            }
            public IEnumerator GetEnumerator()
            {
                yield return new TestCaseData(
                    "",
                    ImmutableArray<KeyValuePair<string, string>>.Empty); 
                yield return new TestCaseData(
                    "]",
                    ImmutableArray<KeyValuePair<string, string>>.Empty);
                yield return new TestCaseData(
                    ",]",
                    ImmutableArray<KeyValuePair<string, string>>.Empty);
                yield return new TestCaseData(
                    "name",
                    CreateExpected(("name", string.Empty)));
                yield return new TestCaseData(
                    "name=",
                    CreateExpected(("name", string.Empty)));
                yield return new TestCaseData(
                    "name=,",
                    CreateExpected(("name", string.Empty)));
                yield return new TestCaseData(
                    "name=value",
                    CreateExpected(("name", "value")));
                yield return new TestCaseData(
                    "name=value,",
                    CreateExpected(("name", "value")));
                yield return new TestCaseData(
                    "name=$832,",
                    CreateExpected(("name", "$832")));
                yield return new TestCaseData(
                    "name=$832,name2, name3=5",
                    CreateExpected(("name", "$832"), ("name2", string.Empty), ("name3", "5")));
                yield return new TestCaseData(
                    "name=$832,name2], name3=5",
                    CreateExpected(("name", "$832"), ("name2", string.Empty)));
            }
        }
        [TestCaseSource(typeof(ExtractSource))]
        public void GivenJustName_ReturnsNameAndEmptyValue(string input, ImmutableArray<KeyValuePair<string, string>> expected)
        {
            var actual = ArrayContentExtractor.Extract(input);

            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}