using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Antlr4.Runtime;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Label = Righthand.RetroDbgDataProvider.Models.Parsing.Label;

namespace Righthand.RetroDbgDataProvider.Test.KickAssembler.Services.Implementation;

[TestFixture]
public class KickAssemblerSourceCodeParserTest : BaseTest<KickAssemblerSourceCodeParser>
{
    private readonly DateTimeOffset _now = new DateTimeOffset(2024, 10, 13, 18, 22, 15, TimeSpan.Zero);
    private Stream GetStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    ImmutableDictionary<string, string> CreateStructure(params KeyValuePair<string, string>[] files)
    {
        var result = ImmutableDictionary<string, string>.Empty.AddRange(files);
        var fileService = Fixture.Freeze<IFileService>();
        fileService.FileExists(Arg.Any<string>()).Returns(x => result.ContainsKey((string)x[0]));
        fileService.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult(result[(string)x[0]]));
        return result;
    }


    [TestFixture]
    public class ParseStream : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void WhenEmptyContent_ResultWithCorrectNameAndNoReferencesIsReturned()
        {
            var actual = Target.ParseStream("d:/root/test.asm", "", new AntlrInputStream(), _now,
                FrozenSet<string>.Empty, [], liveContent: null);

            Assert.That(actual.FileName, Is.EqualTo("d:/root/test.asm"));
            Assert.That(actual.ReferencedFiles, Is.Empty);
        }

        [Test]
        public void GivenSampleWithoutReference_ResultWithNoReferencesIsReturned()
        {
            const string sample = """
                                  lda #5                            
                                  """;
            var actual = Target.ParseStream("test.asm", "", new AntlrInputStream(sample),
                _now, FrozenSet<string>.Empty, [], liveContent: null);

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
            var actual = Target.ParseStream(rootFile, "", new AntlrInputStream(sample),
                    _now, FrozenSet<string>.Empty, [], liveContent: null)
                .ReferencedFiles
                .Select(r => r.FullFilePath)
                .ToImmutableArray();

            Assert.That(actual, Is.EquivalentTo(new[] { myLibraryPath }));
        }
    }

    [TestFixture]
    public class ParseFileFromMemoryContent : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public void AfterFileContentIsParsed_LiveContentIsAssigned()
        {
            const string code = "lda #5";
            var inMemoryContent = new InMemoryFileContent("filename.asm", code, _now);
            var actual = Target.ParseFileFromMemoryContent("filename.asm", "", inMemoryContent, FrozenSet<string>.Empty,
                ImmutableArray<string>.Empty, oldState: null);

            Assert.That(actual.LiveContent, Is.EqualTo(code));
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
                    [
                        new ReferencedFileInfo(0, 0, "MyLibrary.asm","MyLibrary.asm", FrozenSet<string>.Empty)
                    ], [])
                .Select(r => r.FullFilePath)
                .ToImmutableArray();

            Assert.That(actual, Is.EquivalentTo(ImmutableHashSet<string>.Empty.Add(myLibraryPath)));
        }
    }

    [TestFixture]
    public class InitialParseAsync : KickAssemblerSourceCodeParserTest
    {
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
        public async Task
            GivenSampleWithSingleIncludedFile_AndFileExistsInProjectAndLibrary_ParsedFilesContainsIncludedFileFromProject()
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
        public async Task
            GivenSampleWithSingleIncludedFile_AndFileExistsInLibrary_ParsedFilesContainsIncludedFileFromLibrary()
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

    [TestFixture]
    public class ParseInternalAsync : KickAssemblerSourceCodeParserTest
    {
        [Test]
        public async Task WhenSameFileIsIncludedWithTwoDifferentDefineSets_BothFilesAreParsed()
        {
            var mainAsm = Path.Combine("project", "main.asm");
            var includedAsm = Path.Combine("project", "multi_import.asm");
            var files = CreateStructure(
                new(
                    mainAsm, """
                             #define ONE
                             #import "multi_import.asm"
                             #define TWO
                             #import "multi_import.asm"
                             """
                ),
                new(
                    includedAsm, """
                                 lda #5
                                 """
                )
            );

            await Target.ParseInternalAsync(mainAsm, FrozenDictionary<string, InMemoryFileContent>.Empty,
                FrozenSet<string>.Empty, []);
            ImmutableArray<string> expected = [mainAsm, includedAsm];

            Assert.That(Target.AllFiles.Keys, Is.EquivalentTo(expected));
        }

        [Test]
        public async Task WhenSameFileIsIncludedWithTwoDifferentDefineSets_ImportedFileHasBothDefineSets()
        {
            var mainAsm = Path.Combine("project", "main.asm");
            var includedAsm = Path.Combine("project", "multi_import.asm");
            var files = CreateStructure(
                new(
                    mainAsm, """
                             #define ONE
                             #import "multi_import.asm"
                             #define TWO
                             #import "multi_import.asm"
                             """
                ),
                new(
                    includedAsm, """
                                 lda #5
                                 """
                )
            );

            await Target.ParseInternalAsync(mainAsm, FrozenDictionary<string, InMemoryFileContent>.Empty,
                FrozenSet<string>.Empty, []);
            var actual = Target.AllFiles.GetValueOrDefault(includedAsm)!
                .AllDefineSets;

            Assert.That(actual.Length, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class LoadReferencedFilesAsync : KickAssemblerSourceCodeParserTest
    {
        (KickAssemblerLexer Lexer, CommonTokenStream Stream, KickAssemblerParser Parser, 
            KickAssemblerParserListener
            ParserListener,
            KickAssemblerLexerErrorListener LexerErrorListener, 
            KickAssemblerParserErrorListener ParserErrorListener,
            ImmutableArray<IToken> AllTokens)
            GetParser(string text,
                params string[] definitions)
        {
            var input = new AntlrInputStream(text);
            var lexer = new KickAssemblerLexer(input)
            {
                DefinedSymbols = definitions.ToHashSet(),
            };
            var lexerErrorListener = new KickAssemblerLexerErrorListener();
            lexer.AddErrorListener(lexerErrorListener);
            var stream = new CommonTokenStream(lexer);
            var parserErrorListener = new KickAssemblerParserErrorListener();
            var parserListener = new KickAssemblerParserListener();
            var parser = new KickAssemblerParser(stream)
            {
                BuildParseTree = true,
            };
            parser.AddParseListener(parserListener);
            parser.AddErrorListener(parserErrorListener);
            ImmutableArray<IToken> allTokens = [..stream.GetTokens()];
            return (lexer, stream, parser, parserListener, lexerErrorListener, parserErrorListener, allTokens);
        }

        [Test]
        public async Task WhenNoReferences_DoesNotLoadAny()
        {
            var mainParsed = GetParser("""
                                       lda #5
                                       """);
            var source = new KickAssemblerParsedSourceFile("main.asm", "", mainParsed.AllTokens,
                FrozenDictionary<IToken, ReferencedFileInfo>.Empty, FrozenSet<string>.Empty,
                FrozenSet<string>.Empty, Scope.Empty, 
                _now, liveContent: null, isImportOnce: false, 
                mainParsed.LexerErrorListener.Errors, mainParsed.ParserErrorListener.Errors, mainParsed.ParserListener.SyntaxErrors);
            var parsed = new ModifiableParsedFilesIndex<KickAssemblerParsedSourceFile>();
            var oldState =
                new ImmutableParsedFilesIndex<KickAssemblerParsedSourceFile>(
                    FrozenDictionary<string, IImmutableParsedFileSet<KickAssemblerParsedSourceFile>>.Empty);

            await Target.LoadReferencedFilesAsync(parsed, source, FrozenDictionary<string, InMemoryFileContent>.Empty,
                [], oldState, CancellationToken.None);

            Assert.That(parsed.Files.Count, Is.Zero);
        }

        [Test]
        public async Task WhenSimpleReferences_ItIsLoaded()
        {
            var mainParsed = GetParser("""
                                       lda #5
                                       #import "test.asm"
                                       """);
            var referencedFiles = new Dictionary<IToken, ReferencedFileInfo>
            {
                {
                    Fixture.Create<IToken>(),
                    new ReferencedFileInfo(2, 0, "test.asm","test.asm", FrozenSet<string>.Empty, "test.asm")
                }
            }.ToFrozenDictionary();
            var source = new KickAssemblerParsedSourceFile("main.asm", "", mainParsed.AllTokens,
                referencedFiles,
                FrozenSet<string>.Empty,
                FrozenSet<string>.Empty, 
                Scope.Empty, 
                _now,
                liveContent: null, 
                isImportOnce: false, 
                mainParsed.LexerErrorListener.Errors, mainParsed.ParserErrorListener.Errors, mainParsed.ParserListener.SyntaxErrors);
            var parsed = new ModifiableParsedFilesIndex<KickAssemblerParsedSourceFile>();
            var oldState =
                new ImmutableParsedFilesIndex<KickAssemblerParsedSourceFile>(
                    FrozenDictionary<string, IImmutableParsedFileSet<KickAssemblerParsedSourceFile>>.Empty);

            var inMemoryContent =
                new Dictionary<string, InMemoryFileContent>
                {
                    { "test.asm", new InMemoryFileContent("test.asm", "yolo", _now) }
                }.ToFrozenDictionary();
            await Target.LoadReferencedFilesAsync(parsed, source, inMemoryContent,
                [], oldState, CancellationToken.None);

            Assert.That(parsed.Files.Count, Is.EqualTo(1));
            Assert.That(parsed.Files.Single().Key, Is.EqualTo("test.asm"));
        }
    }
}