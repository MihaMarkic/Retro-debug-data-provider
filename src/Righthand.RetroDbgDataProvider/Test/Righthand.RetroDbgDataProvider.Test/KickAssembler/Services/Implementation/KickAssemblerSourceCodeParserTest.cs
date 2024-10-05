using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Antlr4.Runtime;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

[TestFixture]
public class KickAssemblerSourceCodeParserTest : BaseTest<KickAssemblerSourceCodeParser>
{
    [TestFixture]
    public class ParseStream : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void WhenEmptyContent_ResultWithCorrectNameAndNoReferencesIsReturned()
        {
            var actual = Target.ParseStream("d:/root/test.asm", new MemoryStream(), DateTimeOffset.Now,
                FrozenSet<string>.Empty, []);

            Assert.That(actual.FileName, Is.EqualTo("d:/root/test.asm"));
            Assert.That(actual.ReferencedFiles, Is.Empty);
        }

        [Test]
        public void GivenSampleWithoutReference_ResultWithNoReferencesIsReturned()
        {
            const string sample = """
                                  lda #5                            
                                  """;
            var actual = Target.ParseStream("test.asm", new MemoryStream(Encoding.UTF8.GetBytes(sample)),
                DateTimeOffset.Now, FrozenSet<string>.Empty, []);

            Assert.That(actual.ReferencedFiles, Is.Empty);
        }

        [Test]
        public void GivenSampleWithSingleReference_ResultWithThatReferencesIsReturned()
        {
            const string sample = """
                                  #import "MyLibrary.asm" 

                                  lda #5                            
                                  """;
            var fileService = Fixture.Freeze<IFileService>();
            var myLibraryPath = Path.Combine("d:", "root", "MyLibrary.asm");
            fileService.FileExists(myLibraryPath).Returns(true);
            var rootFile = Path.Combine("d:", "root", "test.asm");
            var actual = Target.ParseStream(rootFile, new MemoryStream(Encoding.UTF8.GetBytes(sample)),
                DateTimeOffset.Now, FrozenSet<string>.Empty, []);

            Assert.That(actual.ReferencedFiles, Is.EquivalentTo(new[] { myLibraryPath }));
        }
    }

    [TestFixture]
    public class GetAbsolutePaths : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void WhenSingleLibraryExistsRelativeToFile_ReturnsAbsolutePath()
        {
            var myLibraryPath = Path.Combine("d:", "root", "MyLibrary.asm");
            var rootPath = Path.Combine("d:", "root");
            var fileService = Fixture.Freeze<IFileService>();
            fileService.FileExists(myLibraryPath).Returns(true);
            var actual = Target.GetAbsolutePaths(rootPath, ["MyLibrary.asm"], []);

            Assert.That(actual, Is.EquivalentTo(ImmutableHashSet<string>.Empty.Add(myLibraryPath)));
        }
    }
}