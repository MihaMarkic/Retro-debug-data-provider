using System.Collections.Frozen;
using System.Collections.Immutable;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.Services.Implementation;

public class SourceCodeParserTest: BaseTest<SourceCodeParser<ParsedSourceFile>>
{
    [TestFixture]
    public class GetMatchingFrozenSet : SourceCodeParserTest
    {
        [Test]
        public void GivenArrayContainsSet_FindsSetAndReturnsTrue()
        {
            var sourceSet = new HashSet<string>(["two"]).ToFrozenSet();
            ImmutableArray<FrozenSet<string>> array = [
                new HashSet<string>(["one"]).ToFrozenSet(),
                sourceSet,
                new HashSet<string>(["three"]).ToFrozenSet()
            ];
            var searchSet = new HashSet<string>(["two"]).ToFrozenSet();
            
            var actual = SourceCodeParser<ParsedSourceFile>.GetMatchingFrozenSet(array, searchSet, out var matching);
            
            
            Assert.That(actual, Is.True);
            Assert.That(matching, Is.SameAs(sourceSet));
        }
        [Test]
        public void GivenArrayDoesNotContainSet_ReturnsFalse()
        {
            ImmutableArray<FrozenSet<string>> array = [
                new HashSet<string>(["one"]).ToFrozenSet(),
                new HashSet<string>(["three"]).ToFrozenSet()
            ];
            var searchSet = new HashSet<string>(["two"]).ToFrozenSet();
            
            var actual = SourceCodeParser<ParsedSourceFile>.GetMatchingFrozenSet(array, searchSet, out var matching);
            
            
            Assert.That(actual, Is.False);
        }
    }
}