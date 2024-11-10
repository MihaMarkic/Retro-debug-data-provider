using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

public class KickAssemblerCompilerTest: BaseTest<KickAssemblerCompiler>
{
    [TestFixture]
    public class CreateProcessArguments : KickAssemblerCompilerTest
    {
        [Test]
        public void WhenNoLibDirPresentInSettings_LibDirIsNotInResult()
        {
            var settings = new KickAssemblerCompilerSettings(KickAssemblerPath: null, LibDirs: []);

            var actual = KickAssemblerCompiler.CreateProcessArguments("file.asm", "output_dir", settings);
            
            Assert.That(actual, Does.Not.Contain("-libdir"));
        }
        [Test]
        public void WhenSingleLibDirIsPresentInSettings_LibDirIsInResult()
        {
            var settings = new KickAssemblerCompilerSettings(KickAssemblerPath: null, LibDirs: ["dir1"]);

            var actual = KickAssemblerCompiler.CreateProcessArguments("file.asm", "output_dir", settings);
            
            Assert.That(actual, Does.Contain("-libdir \"dir1\""));
        }
        [Test]
        public void WhenTwoLibDirArePresentInSettings_BothLibDirAreInResult()
        {
            var settings = new KickAssemblerCompilerSettings(KickAssemblerPath: null, LibDirs: ["dir1", "dir2"]);

            var actual = KickAssemblerCompiler.CreateProcessArguments("file.asm", "output_dir", settings);
            
            Assert.That(actual, Does.Contain("-libdir \"dir1\""));
            Assert.That(actual, Does.Contain("-libdir \"dir2\""));
        }
    }

    [TestFixture]
    public class ExtrapolateErrorLength : KickAssemblerCompilerTest
    {
        [TestCase("_DEFAULT_", ExpectedResult = new int[] { 0, 1 })]
        [TestCase("Can't open file: one_main.asmx", ExpectedResult = new int[] { 0, 13 })]
        public int[] GivenErrorText_SetsOffsetAndLengthAccordingly(string errorText)
        {
            var actual = KickAssemblerCompiler.ExtrapolateErrorLength(errorText);

            return [actual.Offset, actual.Length];
        }
    }
}