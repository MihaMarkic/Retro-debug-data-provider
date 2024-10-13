using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Antlr4.Runtime;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

[TestFixture]
public class KickAssemblerSourceCodeParserTest : BaseTest<KickAssemblerSourceCodeParser>
{
    private Stream GetStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));
    [TestFixture]
    public class ParseStream : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void WhenEmptyContent_ResultWithCorrectNameAndNoReferencesIsReturned()
        {
            var actual = Target.ParseStream("d:/root/test.asm", new AntlrInputStream(), DateTimeOffset.Now,
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
            var actual = Target.ParseStream("test.asm", new AntlrInputStream(sample),
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
            var actual = Target.ParseStream(rootFile, new AntlrInputStream(sample),
                DateTimeOffset.Now, FrozenSet<string>.Empty, [])
                .ReferencedFiles
                .Select(r => r.FullFilePath)
                .ToImmutableArray();

            Assert.That(actual, Is.EquivalentTo(new[] { myLibraryPath }));
        }
    }

    [TestFixture]
    public class FillAbsolutePaths : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void WhenSingleLibraryExistsRelativeToFile_ReturnsAbsolutePath()
        {
            var myLibraryPath = Path.Combine("d:", "root", "MyLibrary.asm");
            var rootPath = Path.Combine("d:", "root");
            var fileService = Fixture.Freeze<IFileService>();
            fileService.FileExists(myLibraryPath).Returns(true);
            
            var actual = Target.FillAbsolutePaths(rootPath,
                [new ReferencedFileInfo(0, 0, "MyLibrary.asm", FrozenSet<string>.Empty)], [])
                .Select(r => r.FullFilePath)
                .ToImmutableArray();

            Assert.That(actual, Is.EquivalentTo(ImmutableHashSet<string>.Empty.Add(myLibraryPath)));
        }
    }

    [TestFixture]
    public class InitialParseAsync : KickAssemblerSourceCodeParserTest
    {
        ImmutableDictionary<string, string> CreateStructure(params KeyValuePair<string, string>[] files)
        {
            var result = ImmutableDictionary<string, string>.Empty.AddRange(files);
            var fileService = Fixture.Freeze<IFileService>();
            fileService.FileExists(Arg.Any<string>()).Returns(x => result.ContainsKey((string)x[0]));
            fileService.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(result[(string)x[0]]));
            return result;
        }

        [Test]
        public async Task GivenSampleWithSingleIncludedFile_ParseFilesContainsBothFiles()
        {
            var mainAsm = Path.Combine("project", "main.asm");
            var includedAsm = Path.Combine("project", "included.asm");
            var files = CreateStructure(
                new(
                    mainAsm, """
                             #import "included.asm"
                             """
                ),
                new(
                    includedAsm, """
                                 lda #5
                                 """
                )
            );
            
            await Target.InitialParseAsync("project", FrozenDictionary<string, InMemoryFileContent>.Empty,
                FrozenSet<string>.Empty, ImmutableArray<string>.Empty);
            
            ImmutableArray<string> expected = [mainAsm, includedAsm];
            
            Assert.That(Target.AllFiles.Keys, Is.EquivalentTo(expected));
        }
        [Test]
        public async Task GivenSampleWithSingleIncludedFile_AndFileExistsInProjectAndLibrary_ParsedFilesContainsIncludedFileFromProject()
        {
            var mainAsm = Path.Combine("project", "main.asm");
            var includedAsm = Path.Combine("project", "included.asm");
            var lib1Directory = "lib1";
            var libraryIncludedAsm = Path.Combine(lib1Directory, "included.asm");
            var files = CreateStructure(
                new(
                    mainAsm, """
                             #import "included.asm"
                             """
                ),
                new(
                    includedAsm, """
                                 lda #5
                                 """
                ),
                new(
                    libraryIncludedAsm, """
                                        lda #1
                                        """
                )
            );
            
            await Target.InitialParseAsync("project", FrozenDictionary<string, InMemoryFileContent>.Empty,
                FrozenSet<string>.Empty, [lib1Directory]);
            ImmutableArray<string> expected = [mainAsm, includedAsm];
            
            Assert.That(Target.AllFiles.Keys, Is.EquivalentTo(expected));
        }
        [Test]
        public async Task GivenSampleWithSingleIncludedFile_AndFileExistsInLibrary_ParsedFilesContainsIncludedFileFromLibrary()
        {
            var mainAsm = Path.Combine("project", "main.asm");
            var lib1Directory = "lib1";
            var libraryIncludedAsm = Path.Combine(lib1Directory, "included.asm");
            var files = CreateStructure(
                new(
                    mainAsm, """
                             #import "included.asm"
                             """
                ),
                new(
                    libraryIncludedAsm, """
                                        lda #1
                                        """
                )
            );
            
            await Target.InitialParseAsync("project", FrozenDictionary<string, InMemoryFileContent>.Empty,
                FrozenSet<string>.Empty, [lib1Directory]);
            ImmutableArray<string> expected = [mainAsm, libraryIncludedAsm];
            
            Assert.That(Target.AllFiles.Keys, Is.EquivalentTo(expected));
        }
    }
}