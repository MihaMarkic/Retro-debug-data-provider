using System.Collections;
using System.Collections.Frozen;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.CompletionOptionCollectors;

public class ArrayPropertiesCompletionOptionsTest
{
    [TestFixture]
    public class GetOption : ArrayPropertiesCompletionOptionsTest
    {
        public class TestSource : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                yield return new TestCaseData(
                    ".file c64 [",
                    ArrayProperties.GetNames(".file"));
                yield return new TestCaseData(
                    ".file c64 [mbfiles, name=tubo",
                    new HashSet<string>(["type"]));
            }
        }

        [TestCaseSource(typeof(TestSource))]
        public void GivenSamples_ReturnsNames(string line, ISet<string> expected)
        {
            var actual =
                    ArrayPropertiesCompletionOptions.GetOption(line, 0, line.Length, line.Length - 1)
                    ?.Values;
        
            Assert.That(actual, Is.EquivalentTo(expected.ToFrozenSet()));
        }
    }

    [TestFixture]
    public class IsCursorWithinArrayKeyword : ArrayPropertiesCompletionOptionsTest
    {
        
    }
}