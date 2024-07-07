using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using System.Collections.Immutable;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

public class KickAssemblerByteDumpParserTest: BaseTest<KickAssemblerByteDumpParser>
{
    [TestFixture]
    public class SplitAssemblyLine: KickAssemblerByteDumpParserTest
    {
        [Test]
        public void GivenFullLine_SplitsCorrectly()
        {
            var actual = Target.SplitAssemblyLine("0801: 0c 08 b5 07 9e 20 32 30 36 32 00 00 00           -         .byte $0c,$08,$b5,$07,$9e,$20,$32,$30,$36,$32,$00,$00,$00");

            Assert.That(actual.Data.ToString(), Is.EqualTo("0801: 0c 08 b5 07 9e 20 32 30 36 32 00 00 00"));
            Assert.That(actual.Instructions.ToString(), Is.EqualTo(".byte $0c,$08,$b5,$07,$9e,$20,$32,$30,$36,$32,$00,$00,$00"));
        }
        [Test]
        public void GivenEdgeCaseWithEmptyDescription_SplitsCorrectly()
        {
            var actual = Target.SplitAssemblyLine("0801: 0c -");

            Assert.That(actual.Data.ToString(), Is.EqualTo("0801: 0c"));
            Assert.That(actual.Instructions.ToString(), Is.Empty);
        }
        [Test]
        public void GivenOnlyData_SplitsCorrectly()
        {
            var actual = Target.SplitAssemblyLine("0801: 0c");

            Assert.That(actual.Data.ToString(), Is.EqualTo("0801: 0c"));
            Assert.That(actual.Instructions.ToString(), Is.Empty);
        }
    }
    [TestFixture]
    public class ReadAssemblyLine : KickAssemblerByteDumpParserTest
    {
        [Test]
        public void GivenSampleLine_ParsesAddressCorrectly()
        {
            var actual = Target.ReadAssemblyLine("081f: a0 00", "main:   ldy #0");

            Assert.That(actual.Address, Is.EqualTo(0x081f));
        }
        [Test]
        public void GivenSampleLine_ParsesBytesCorrectly()
        {
            var actual = Target.ReadAssemblyLine("081f: a0 00", "main:   ldy #0");

            Assert.That(actual.Data, Is.EqualTo(new byte[] { 0xa0, 0x00 }));
        }
        [Test]
        public void GivenSampleLine_ParsesDescriptionCorrectly()
        {
            var actual = Target.ReadAssemblyLine("081f: a0 00", "main:   ldy #0");

            Assert.That(actual.Description, Is.EqualTo("main:   ldy #0"));
        }
    }
    [TestFixture]
    public class ParseContent : KickAssemblerByteDumpParserTest
    {
        [Test]
        public void WhenEmptyContent_Throws()
        {
            Assert.Throws<Exception>(() => Target.ParseContent(""));
        }
        [Test]
        public void WhenOnlySegmentName_Throws()
        {
            Assert.Throws<Exception>(() => Target.ParseContent("******************************* Segment: Default *******************************"));
        }
        [Test]
        public void WhenOnlySegmentAndBlockHeader_ReturnsSegmentWithEmptyBlock()
        {
            const string source = """
                ******************************* Segment: Default *******************************
                [Unnamed]
                """;
            var actual = Target.ParseContent(source);

            Assert.That(actual.Length, Is.EqualTo(1));
            var segment = actual.Single();
            Assert.That(segment.Blocks.Length, Is.EqualTo(1));
            var block = segment.Blocks.Single();
            Assert.That(block.Lines, Is.Empty);
        }
        [Test]
        public void WhenOnlySegmentAndBlockHeader_ReturnsCorrectlyNameSegmentAndBlock()
        {
            const string source = """
        ******************************* Segment: Default *******************************
        [Unnamed]
        """;
            var actual = Target.ParseContent(source);

            Assert.That(actual.Length, Is.EqualTo(1));
            var segment = actual.Single();
            Assert.That(segment.Name, Is.EqualTo("Default"));
            var block = segment.Blocks.Single();
            Assert.That(block.Name, Is.EqualTo("Unnamed"));
        }
        [Test]
        public void GivenRealSample_ParsesWithoutErrors()
        {
            var sample = LoadKickAssSample("ByteDump.txt");

            var actual = Target.ParseContent(sample);
        }
        [Test]
        public void GivenRealSample_ReturnsCorrectSegmentCount()
        {
            var sample = LoadKickAssSample("ByteDump.txt");

            var actual = Target.ParseContent(sample);

            Assert.That(actual.Length, Is.EqualTo(3));
        }
        [Test]
        public void GivenRealSample_SegmentNamesAreCorrect()
        {
            var sample = LoadKickAssSample("ByteDump.txt");

            var actual = Target.ParseContent(sample);

            Assert.That(actual.Select(s => s.Name), Is.EqualTo(new string[] { "Default", "xtest", "AdditionalEmpty" }));
        }
        [Test]
        public void GivenRealSample_LastSegmentBlockLinesAddressesAreCorrect()
        {
            var sample = LoadKickAssSample("ByteDump.txt");

            var actual = Target.ParseContent(sample);

            Assert.That(
                actual.Last().Blocks.Single().Lines.Select(l => l.Address), 
                Is.EqualTo(new ushort[] { 0x1000, 0x1020, 0x1040 }));
        }
        [Test]
        public void GivenRealSample_LastSegmentBlockLinesHave96Zeros()
        {
            var sample = LoadKickAssSample("ByteDump.txt");

            var actual = Target.ParseContent(sample);

            var lines = actual.Last().Blocks.Single().Lines.SelectMany(l => l.Data).ToImmutableArray();

            Assert.That(lines.Length, Is.EqualTo(96));
            Assert.That(lines.All(d => d == 0x00), Is.True);
        }
    }
}
