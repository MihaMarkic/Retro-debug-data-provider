using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models.Program;
using KickAss = Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

internal class KickAssemblerProgramInfoBuilderTest: BaseTest<KickAssemblerProgramInfoBuilder>
{
    [TestFixture]
    public class BuildAppInfo: KickAssemblerProgramInfoBuilderTest
    {
        public class SampleFile : BuildAppInfo
        {
            IKickAssemblerDbgParser parser = default!;
            KickAss.C64Debugger dbg = default!;

            [SetUp]
            public async Task SetupAsync()
            {
                parser = new KickAssemblerDbgParser(Substitute.For<ILogger<KickAssemblerDbgParser>>());
                var sample = LoadKickAssSample("FullSample.dbg");
                dbg = await parser.LoadContentAsync(sample, "path");
            }

            [Test]
            public async Task GivenSampleFile_DefaultSegmentHasBreakpoint()
            {
                var actual = await Target.BuildAppInfoAsync(dbg, default);

                var defaultSegment = actual.Segments["Default"];
                var breakpoints = defaultSegment.Breakpoints;

                var expected = new Breakpoint[]
                {
                    new Breakpoint(0x2002, "if y<5"),
                };
                Assert.That(breakpoints, Is.EqualTo(expected));
            }
            [Test]
            public async Task GivenSampleFile_AdditionalEmptySegmentHasNoBreakpoints()
            {
                var actual = await Target.BuildAppInfoAsync(dbg, default);

                var defaultSegment = actual.Segments["AdditionalEmpty"];

                Assert.That(defaultSegment.Breakpoints, Is.Empty);
            }
        }
    }
}
