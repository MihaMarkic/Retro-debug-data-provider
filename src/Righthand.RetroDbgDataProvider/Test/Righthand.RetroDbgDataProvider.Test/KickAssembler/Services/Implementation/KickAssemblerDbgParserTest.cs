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
            var actual = KickAssemblerDbgParser.ParseSource("0,KickAss.jar:/include/auto_include.asm");

            Assert.That(actual,
                Is.EqualTo(new Source(0, SourceOrigin.KickAss, "/include/auto_include.asm")));
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
    [TestFixture]
    public class ParseBlockItem : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenCorrectValue_ParsesCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseBlockItem("$0801,$0802,0,56,2,56,6");

            Assert.That(
                actual, Is.EqualTo(
                    new BlockItem(0x0801, 0x0802, new FileLocation(0, 56, 2, 56, 6))));
        }
    }
    [TestFixture]
    public class ParseLabel : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenCorrectValue_ParsesCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseLabel("Default,$4000,start,1,5,1,5,6");

            Assert.That(actual, Is.EqualTo(
                new Label("Default", 0x4000, "start", new FileLocation(1, 5, 1, 5, 6))
                ));
        }
    }
    [TestFixture]
    public class ParseBreakpoint : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenCorrectValue_ParsesCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseBreakpoint("Default,$2002,if y&lt;5");

            Assert.That(actual, Is.EqualTo(new Breakpoint("Default", 0x2002, "if y<5")));
        }
    }
    [TestFixture]
    public class ParseWatchpoint : KickAssemblerDbgParserTest
    {
        [Test]
        public void GivenCorrectValue_ParsesCorrectly()
        {
            var actual = KickAssemblerDbgParser.ParseWatchpoint("Default,$2000,$2002,store");

            Assert.That(actual, Is.EqualTo(new Watchpoint("Default", 0x2000, 0x2002, "store")));
        }
    }
    [TestFixture]
    public class LoadContentAsync : KickAssemblerDbgParserTest
    {
        [Test]
        public async Task GivenSampleFile_ParsesCorrectly()
        {
            var sample = LoadKickAssSample("FullSample.dbg");
            var actual = await Target.LoadContentAsync(sample,"path");

            Assert.That(actual.Sources.Length, Is.EqualTo(3));
            Assert.That(actual.Segments.Length, Is.EqualTo(2));
            var defaultSegment = actual.Segments[0];
            Assert.That(defaultSegment.Name, Is.EqualTo("Default"));
            Assert.That(defaultSegment.Blocks.Length, Is.EqualTo(3));
            var basicBlock = defaultSegment.Blocks[0];
            Assert.That(basicBlock.Name, Is.EqualTo("Basic"));
            Assert.That(actual.Labels.Length, Is.EqualTo(3));
            Assert.That(actual.Breakpoints.Length, Is.EqualTo(1));
            Assert.That(actual.Watchpoints.Length, Is.EqualTo(1));
        }
    }
}
