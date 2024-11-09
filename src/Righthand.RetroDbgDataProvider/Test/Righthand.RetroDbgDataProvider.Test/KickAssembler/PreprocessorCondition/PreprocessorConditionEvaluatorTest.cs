using System.Collections.Frozen;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.PreprocessorCondition;

public class PreprocessorConditionEvaluatorTest
{
    private FrozenSet<string> CreateSymbols(params string[] symbols)
    {
        return symbols.ToFrozenSet();
    }
    private FrozenSet<string> CreateSymbols(string symbols)
    {
        return symbols.Split(';').Select(s => s.Trim()).ToFrozenSet();
    }
    [TestFixture]
    public class IsDefined: PreprocessorConditionEvaluatorTest
    {
        [Test]
        public void WhenEmptyInput_ReturnsFalse()
        {
            var actual = PreprocessorConditionEvaluator.IsDefined(CreateSymbols(), "");
            
            Assert.That(actual, Is.False);
        }
        
        [TestCase("DEFINED", "DEFINED", ExpectedResult = true)]
        [TestCase("DEFINED", "", ExpectedResult = false)]
        [TestCase("DEFINED && UNDEFINED", "DEFINED", ExpectedResult = false)]
        public bool WhenEmptyInput_ReturnsFalse(string input, string symbolsList)
        {
            var symbols = CreateSymbols(symbolsList);
            var actual = PreprocessorConditionEvaluator.IsDefined(symbols, input);

            return actual;
        }
    }
}