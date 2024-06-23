using NUnit.Framework;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class GrammarTest: Bootstrap
{
    [TestFixture]
    public class DecNumber: Bootstrap
    {
        [TestCase("55")]
        [TestCase("100")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.decNumber()));
        }

        [TestCase("a55")]
        [TestCase("100b")]
        [TestCase("a100b")]
        [TestCase("aa")]
        [TestCase("")]
        public void TestInvalid(string input)
        {
            Assert.Throws<Exception>(() => Run(input, p => p.decNumber()));
        }
    }

    [TestFixture]
    public class LabelName : Bootstrap
    {
        [TestCase("!")]
        [TestCase("!tubo")]
        [TestCase("tubo")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.labelName()));
        }
    }
    [TestFixture]
    public class CpuDirectiveName : Bootstrap
    {
        [TestCase("cpu _65c02")]
        [TestCase("cpu _6502NoIllegals")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.cpuDirective()));
        }
    }
}