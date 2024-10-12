using NUnit.Framework;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Grammar;

[TestFixture]
public class ParserTest: ParserBootstrap<ParserTest>
{
    [TestFixture]
    public class DecNumber: ParserTest
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
    public class LabelName : ParserTest
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
    public class CpuDirectiveName : ParserTest
    {
        [TestCase(".cpu _65c02")]
        [TestCase(".cpu _6502NoIllegals")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.cpuDirective()));
        }
    }

    [TestFixture]
    public class EncodingDirective : ParserTest
    {
        [TestCase(".encoding \"screencode_mixed\"")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.encodingDirective()));
        }   
    }
    [TestFixture]
    public class Instruction : ParserTest
    {
        [TestCase("ldy #0")]
        public void TestValid(string input)
        {
            Assert.DoesNotThrow(() => Run(input, p => p.instruction()));
        }   
    }

    [TestFixture]
    public class TestAllSamples : ParserTest
    {
        [TestCase("Sample1")]
        [TestCase("Sample2")]
        [TestCase("Sample3")]
        public void TestValid(string input)
        {
            var content = LoadKickAssSample($"{input}.asm");
            Assert.DoesNotThrow(() => Run(content, p => p.program()));
        }
    }
}