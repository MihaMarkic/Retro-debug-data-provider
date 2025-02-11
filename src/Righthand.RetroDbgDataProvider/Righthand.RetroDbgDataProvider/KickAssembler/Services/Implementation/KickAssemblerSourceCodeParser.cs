using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

/// <summary>
/// Provides parsing of the KickAssembler project's source code.
/// </summary>
/// <remarks>Not thread safe.</remarks>
public sealed class KickAssemblerSourceCodeParser : SourceCodeParser<KickAssemblerParsedSourceFile>,
    IKickAssemblerSourceCodeParser
{
    private readonly ILogger<KickAssemblerSourceCodeParser> _logger;
    private readonly IFileService _fileService;

    private CancellationTokenSource? _parsingCts;
    private string? _projectDirectory;
    private string? _mainFile;

    /// <summary>
    /// Creates a new instance of <see cref="KickAssemblerSourceCodeParser"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileService"></param>
    public KickAssemblerSourceCodeParser(ILogger<KickAssemblerSourceCodeParser> logger, IFileService fileService)
        : base(ImmutableParsedFilesIndex<KickAssemblerParsedSourceFile>.Empty)
    {
        _logger = logger;
        _fileService = fileService;
    }

    /// <inheritdoc cref="ISourceCodeParser"/>
    public async Task InitialParseAsync(string projectDirectory,
        FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Initialized in directory {ProjectDirectory} with libraries {Libraries} and symbols {DefineSymbols}",
            projectDirectory, string.Join(",", libraryDirectories), string.Join(";", inDefines));
        _projectDirectory = projectDirectory;
        _mainFile = Path.Combine(_projectDirectory, "main.asm");
        await ParseAsync(inMemoryFilesContent, inDefines, libraryDirectories, ct).ConfigureAwait(false);
        _logger.LogInformation("Initial parsing done");
    }

    /// <inheritdoc cref="ISourceCodeParser{T}.ParseAsync"/>
    public Task ParseAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
    {
        if (_mainFile is null)
        {
            _logger.LogError("Not initialized");
            throw new Exception("KickAssemblerSourceCodeParser has not been initialized");
        }

        ParsingTask = ParseInternalAsync(_mainFile, inMemoryFilesContent, inDefines, libraryDirectories, ct);
        return ParsingTask;
    }

    /// <summary>
    /// Initial parsing point that prepares all objects for parsing.
    /// </summary>
    /// <param name="mainFile"></param>
    /// <param name="inMemoryFilesContent"></param>
    /// <param name="inDefines"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="ct"></param>
    /// <exception cref="Exception"></exception>
    internal async Task ParseInternalAsync(string mainFile, FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting parsing task");
            await StopAsync();

            _parsingCts = new();
            using (var linkedCancellationSource =
                   CancellationTokenSource.CreateLinkedTokenSource(_parsingCts.Token, ct))
            {
                ModifiableParsedFilesIndex<KickAssemblerParsedSourceFile> parsed = new();
                await ParseAllFilesAsync(parsed, mainFile, "", inMemoryFilesContent, inDefines, libraryDirectories,
                    AllFiles, linkedCancellationSource.Token);
                await AssignNewFilesAsync(parsed.ToImmutable(), ct);
                _logger.LogInformation("Done parsing");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Parsing task was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing");
        }
    }

    /// <inheritdoc cref="ISourceCodeParser{T}.StopAsync"/>
    public async Task StopAsync()
    {
        if (_parsingCts is not null)
        {
            _logger.LogInformation("Issuing cancellation");
            _parsingCts.Cancel();
            _parsingCts.Dispose();
            _parsingCts = null;
        }

        if (ParsingTask is not null)
        {
            _logger.LogInformation("Waiting for parser task to finish");
            // _parsingTask shouldn't throw
            await ParsingTask;
            _logger.LogInformation("Parsing task stopped");
        }
    }

    /// <summary>
    /// Parse file given in <param name="filePath" /> argument and all referenced files.
    /// </summary>
    /// <param name="parsed"></param>
    /// <param name="filePath"></param>
    /// <param name="relativePath">Relative path to either project or library, depends on the file origin</param>
    /// <param name="inMemoryFilesContent">File content modified in memory, not saved. Key is full path.</param>
    /// <param name="inDefines"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="oldState"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<KickAssemblerParsedSourceFile?> ParseAllFilesAsync(
        ModifiableParsedFilesIndex<KickAssemblerParsedSourceFile> parsed,
        string filePath, string relativePath,
        FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories,
        IParsedFilesIndex<KickAssemblerParsedSourceFile> oldState,
        CancellationToken ct)
    {
        _logger.LogInformation("Parsing file {FilePath}", filePath);
        try
        {
            KickAssemblerParsedSourceFile parsedFile;
            var fileIndex = oldState.GetValueOrDefault(filePath);
            var oldParsedFile = fileIndex?.GetFile(inDefines);
            if (inMemoryFilesContent.TryGetValue(filePath, out var inMemoryContent))
            {
                parsedFile = ParseFileFromMemoryContent(filePath, relativePath, inMemoryContent, inDefines, libraryDirectories, oldParsedFile);
            }
            else if (_fileService.FileExists(filePath))
            {
                parsedFile = await ParseFileAsync(filePath, relativePath, inDefines, libraryDirectories, oldParsedFile, ct).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Can't find start file {StartFile} in project's directory {Directory}", filePath,
                    _projectDirectory);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            if (fileIndex is null)
            {
            }

            parsed.TryAdd(filePath, inDefines, parsedFile);

            await LoadReferencedFilesAsync(parsed, parsedFile, inMemoryFilesContent, libraryDirectories, oldState, ct)
                .ConfigureAwait(false);

            return parsedFile;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parsing file {FilePath}", filePath);
            throw;
        }

        return null;
    }

    /// <summary>
    /// Loads referenced files and caches them to <param name="parsed"/>.
    /// </summary>
    /// <param name="parsed"></param>
    /// <param name="parsedFile"></param>
    /// <param name="inMemoryFilesContent"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="oldState"></param>
    /// <param name="ct"></param>
    /// <remarks>
    /// It's a bit complex because it has to pay attention to #define, #undef, #if, #elif, #else and #importif
    /// preprocessor directives.
    /// </remarks>
    internal async Task LoadReferencedFilesAsync(
        ModifiableParsedFilesIndex<KickAssemblerParsedSourceFile> parsed,
        KickAssemblerParsedSourceFile parsedFile,
        FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        ImmutableArray<string> libraryDirectories,
        IParsedFilesIndex<KickAssemblerParsedSourceFile> oldState,
        CancellationToken ct)
    {
        // loads all referenced files
        foreach (var referencedFile in parsedFile.ReferencedFiles.Where(rf => rf.FullFilePath is not null))
        {
            // first check whether referenced file already declared #importonce
            // and if, then just update reference to it, no need to parse it again
            if (parsed.TryGetImportOnce(referencedFile.FullFilePath!, out var importOnceReference))
            {
                // updates reference to the file variation (filename, define symbols) that declared #importonce 
                var updatedReference = referencedFile with
                {
                    InDefinesOverrideForImportOnce = importOnceReference.Value.DefineSymbols
                };
                parsedFile.UpdateReferencedFileInfo(referencedFile, updatedReference);
            }
            // then check whether same referenced file with same defined symbols has already been parsed
            // when not, it has to be parsed
            else if (!parsed.ContainsKey(referencedFile.FullFilePath!, referencedFile.InDefines))
            {
                var relativeToProjectOrLibrary = Path.GetDirectoryName(referencedFile.NormalizedRelativeFilePath)!;
                var referencedParsedFile = await ParseAllFilesAsync(parsed, referencedFile.FullFilePath!, relativeToProjectOrLibrary,
                    inMemoryFilesContent, referencedFile.InDefines,
                    libraryDirectories, oldState, ct).ConfigureAwait(false);
                // takes updated define symbols
                if (referencedParsedFile is not null)
                {
                    // when file declared #importonce, store it to referencedParsedFile
                    if (referencedParsedFile.IsImportOnce)
                    {
                        parsed.AddImportOnce(referencedFile.FullFilePath!, referencedParsedFile.OutDefines, referencedParsedFile);
                    }
                }
            }
        }
    }

    private string? GetFilePathFromRelative(string source, string relative, ImmutableArray<string> libraryDirectories)
    {
        string? sourceFileDirectory = Path.GetDirectoryName(source);
        if (sourceFileDirectory is null)
        {
            return null;
        }

        foreach (var directory in libraryDirectories.Insert(0, sourceFileDirectory))
        {
            string filePath = Path.Combine(directory, relative);
            if (_fileService.FileExists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }

    private async Task<KickAssemblerParsedSourceFile> ParseFileAsync(string fileName, string relativePath, FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, KickAssemblerParsedSourceFile? oldState, CancellationToken ct)
    {
        var lastWrite = _fileService.GetLastWriteTime(fileName);
        if (oldState?.LastModified == lastWrite && oldState.LiveContent is null)
        {
            return oldState;
        }

        var content = await _fileService.ReadAllTextAsync(fileName, ct);
        return ParseStream(fileName, relativePath, new AntlrInputStream(content), lastWrite, inDefines, libraryDirectories,
            liveContent: null);
    }

    internal KickAssemblerParsedSourceFile ParseFileFromMemoryContent(string fileName, string relativePath,
        InMemoryFileContent inMemoryFileContent,
        FrozenSet<string> inDefines, ImmutableArray<string> libraryDirectories, KickAssemblerParsedSourceFile? oldState)
    {
        if (oldState is not null &&
            string.Equals(oldState.LiveContent, inMemoryFileContent.Content, StringComparison.Ordinal))
        {
            return oldState;
        }

        return ParseStream(fileName, relativePath, new AntlrInputStream(inMemoryFileContent.Content),
            inMemoryFileContent.LastModified, inDefines, libraryDirectories, liveContent: inMemoryFileContent.Content);
    }

    internal KickAssemblerParsedSourceFile ParseStream(string fileName, string relativePath, AntlrInputStream inputStream,
        DateTimeOffset lastModified,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories,
        string? liveContent)
    {
        _logger.LogInformation("Parsing file {FileName}", fileName);
        var lexer = new KickAssemblerLexer(inputStream)
        {
            DefinedSymbols = inDefines.ToHashSet(),
        };
        var lexerErrorListener = new KickAssemblerLexerErrorListener();
        lexer.AddErrorListener(lexerErrorListener);
        var tokenStream = new CommonTokenStream(lexer);
        var parserErrorListener = new KickAssemblerParserErrorListener();
        var parserListener = new KickAssemblerParserListener();
        var parser = new KickAssemblerParser(tokenStream)
        {
            BuildParseTree = true,
        };
        parser.AddErrorListener(parserErrorListener);
        try
        {
            // InvalidOperationException as a consequence of invalid PopMode can be thrown;
            tokenStream.Fill();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Failed parsing source code for file {FileName} probably because of bad conditional directives #else #endif #elif",
                fileName);
            return new KickAssemblerParsedSourceFile(fileName, relativePath, [],
                MapReferencedFilesToDictionary(lexer.ReferencedFiles, tokenStream.GetTokens()),
                inDefines, outDefines: inDefines, parserListener.SegmentDefinitions, parserListener.LabelDefinitions,
                parserListener.VariableDefinitions, parserListener.ConstantDefinitions, parserListener.EnumValuesDefinitions,
                parserListener.MacroDefinitions,
                lastModified, liveContent,
                lexer.IsImportOnce, lexerErrorListener.Errors, parserErrorListener.Errors);
        }
        finally
        {
            _logger.LogInformation("Lexed file {FileName} has these relative references {References}", fileName,
                string.Join(",", lexer.ReferencedFiles.Select(l => l.NormalizedRelativeFilePath)));
        }

        var tree = parser.program();
        ParseTreeWalker.Default.Walk(parserListener, tree);

        ImmutableArray<IToken> allTokens = [..tokenStream.GetTokens()];
        var absoluteReferencePaths =
            FillAbsolutePaths(Path.GetDirectoryName(fileName)!, [..lexer.ReferencedFiles], libraryDirectories);
        return new KickAssemblerParsedSourceFile(fileName, relativePath, allTokens,
            MapReferencedFilesToDictionary(absoluteReferencePaths, allTokens),
            inDefines, lexer.DefinedSymbols.ToFrozenSet(),
            parserListener.SegmentDefinitions, parserListener.LabelDefinitions,
            parserListener.VariableDefinitions, parserListener.ConstantDefinitions,
            parserListener.EnumValuesDefinitions, parserListener.MacroDefinitions,
            lastModified, liveContent,
            lexer.IsImportOnce,
            lexerErrorListener.Errors, parserErrorListener.Errors);
    }

    internal FrozenDictionary<IToken, ReferencedFileInfo> MapReferencedFilesToDictionary(
        IEnumerable<ReferencedFileInfo> source, IList<IToken> tokens)
    {
        var mapped = source.ToFrozenDictionary(
            fr => tokens.Single(t => t.Line == fr.TokenStartLine && t.Column == fr.TokenStartColumn));
        return mapped;
    }

    internal ImmutableArray<ReferencedFileInfo> FillAbsolutePaths(string filePath,
        ImmutableArray<ReferencedFileInfo> relativeReferences,
        ImmutableArray<string> libraryDirectories)
    {
        var builder = ImmutableArray.CreateBuilder<ReferencedFileInfo>(relativeReferences.Length);
        // makes sure filePath is first directory to look in
        var allDirectories = libraryDirectories.Insert(0, filePath);
        foreach (var reference in relativeReferences)
        {
            var directory = allDirectories.FirstOrDefault(d =>
            _fileService.FileExists(Path.Combine(d, reference.NormalizedRelativeFilePath)));
            if (directory is not null)
            {
                builder.Add(reference with { FullFilePath = Path.Combine(directory, reference.NormalizedRelativeFilePath) });
            }
            else
            {
                builder.Add(reference);
                _logger.LogWarning("Could not find referenced source file {File} from file {Source}", reference, filePath);
            }
        }

        return builder.ToImmutableArray();
    }


    protected override async Task DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            await StopAsync();
        }

        await base.DisposeAsyncCore();
    }
}