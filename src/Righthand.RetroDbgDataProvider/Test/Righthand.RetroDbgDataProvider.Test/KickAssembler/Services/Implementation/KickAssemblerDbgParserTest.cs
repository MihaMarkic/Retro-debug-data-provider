using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

public class KickAssemblerDbgParserTest : BaseTest<KickAssemblerDbgParser>
{
    [TestFixture]
    public class ParseSource : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenSampleKickAssJarLine_ParserCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseSource("0,KickAss.jar:/include/autoinclude.asm");

            Assert.That(actual,
                Is.EqualTo(new Source(0, SourceOrigin.KickAss, "/include/autoinclude.asm")));
        }
        [Test]
        public void GivenSampleUserLine_ParserCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseSource("1,/Users/miha/Projects/c64/vs64/First/src/main.asm");

            Assert.That(actual,
                Is.EqualTo(new Source(1, SourceOrigin.User, "/Users/miha/Projects/c64/vs64/First/src/main.asm")));
        }
    }
    [TestFixture]
    public class ParseHexText : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenCorrectValue_ParsesCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseHexText("$0801", 4);

            Assert.That(actual, Is.EqualTo(0x0801));
        }
    }
}
