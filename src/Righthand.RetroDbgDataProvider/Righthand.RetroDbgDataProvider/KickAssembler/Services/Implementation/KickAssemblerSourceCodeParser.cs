using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.RetroDbgDataProvider.Services.Implementation;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
/// <summary>
/// Provides parsing of the KickAssembler project's source code.
/// </summary>
/// <remarks>Not thread safe.</remarks>
public sealed class KickAssemblerSourceCodeParser : SourceCodeParser<KickAssemblerParsedSourceFile>,
    ISourceCodeParser<KickAssemblerParsedSourceFile>
{
    private readonly ILogger<KickAssemblerSourceCodeParser> _logger;
    private readonly IFileService _fileService;

    private CancellationTokenSource? _parsingCts;
    private string? _projectDirectory;
    private string? _mainFile;
    private Task? _parsingTask;
    /// <summary>
    /// Creates a new instance of <see cref="KickAssemblerSourceCodeParser"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileService"></param>
    public KickAssemblerSourceCodeParser(ILogger<KickAssemblerSourceCodeParser> logger, IFileService fileService)
        :base(ParsedFilesIndex<KickAssemblerParsedSourceFile>.Empty)
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
        _projectDirectory = projectDirectory;
        _allFiles = ParsedFilesIndex<KickAssemblerParsedSourceFile>.Empty;
        _mainFile = Path.Combine(_projectDirectory, "main.asm");
        await ParseAsync(inMemoryFilesContent, inDefines, libraryDirectories, ct).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ISourceCodeParser"/>
    public Task ParseAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
    {
        _parsingTask = ParseInternalAsync(inMemoryFilesContent, inDefines, libraryDirectories, ct);
        return _parsingTask;
    }

    private async Task ParseInternalAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent,
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
                if (_mainFile is null)
                {
                    _logger.LogError("Not initialized");
                    throw new Exception("KickAssemblerSourceCodeParser has not been initialized");
                }

                Dictionary<string, KickAssemblerParsedSourceFile> parsed = new();
                await ParseAllFilesAsync(parsed, _mainFile, inMemoryFilesContent, inDefines, libraryDirectories,
                    AllFiles, linkedCancellationSource.Token).ConfigureAwait(false);
                var newAllFiles = parsed.ToFrozenDictionary();
                AllFiles = new ParsedFilesIndex<KickAssemblerParsedSourceFile>(newAllFiles);
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

    public async Task StopAsync()
    {
        if (_parsingCts is not null)
        {
            _logger.LogInformation("Issuing cancellation");
            await _parsingCts.CancelAsync();
        }

        if (_parsingTask is not null)
        {
            _logger.LogInformation("Waiting for parser task to finish");
            // _parsingTask shouldn't throw
            await _parsingTask;
            _logger.LogInformation("Parsing task stopped");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parsed"></param>
    /// <param name="filePath"></param>
    /// <param name="inMemoryFilesContent">File content modified in memory, not saved. Key is full path.</param>
    /// <param name="inDefines"></param>
    /// <param name="libraryDirectories"></param>
    /// <param name="oldState"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<KickAssemblerParsedSourceFile?> ParseAllFilesAsync(
        Dictionary<string, KickAssemblerParsedSourceFile> parsed,
        string filePath,
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
            var oldParsedFile = oldState.GetValueOrDefault(filePath);
            if (inMemoryFilesContent.TryGetValue(filePath, out var inMemoryContent))
            {
                parsedFile = ParseFile(filePath, inMemoryContent, inDefines, libraryDirectories, oldParsedFile);
            }
            else if (_fileService.FileExists(filePath))
            {
                parsedFile = await ParseFileAsync(filePath, inDefines, libraryDirectories, oldParsedFile, ct);
            }
            else
            {
                _logger.LogWarning("Can't find start file {StartFile} in project's directory {Directory}", filePath,
                    _projectDirectory);
                return null;
            }

            ct.ThrowIfCancellationRequested();

            parsed.Add(filePath, parsedFile);
            // initial define symbols for parsing reference files
            var referenceInDefines = parsedFile.OutDefines;
            foreach (var referencedFile in parsedFile.ReferencedFiles)
            {
                if (!parsed.ContainsKey(referencedFile))
                {
                   var referencedParsedFile = await ParseAllFilesAsync(parsed, referencedFile, inMemoryFilesContent, referenceInDefines,
                        libraryDirectories, oldState, ct).ConfigureAwait(false);
                   // takes updated define symbols
                   if (referencedParsedFile is not null)
                   {
                       referenceInDefines = referencedParsedFile.OutDefines;
                   }
                }
            }

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

    private async Task<KickAssemblerParsedSourceFile> ParseFileAsync(string fileName, FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories, KickAssemblerParsedSourceFile? oldState, CancellationToken ct)
    {
        var lastWrite = _fileService.GetLastWriteTime(fileName);
        if (oldState?.LastModified == lastWrite && oldState.LiveContent is null)
        {
            return oldState;
        }

        var content = await _fileService.ReadAllTextAsync(fileName, ct);
        return ParseStream(fileName, new AntlrInputStream(content), lastWrite, inDefines, libraryDirectories);
    }

    private KickAssemblerParsedSourceFile ParseFile(string fileName, InMemoryFileContent inMemoryFileContent,
        FrozenSet<string> inDefines, ImmutableArray<string> libraryDirectories, KickAssemblerParsedSourceFile? oldState)
    {
        if (oldState is not null &&
            string.Equals(oldState.LiveContent, inMemoryFileContent.Content, StringComparison.Ordinal))
        {
            return oldState;
        }

        return ParseStream(fileName, new AntlrInputStream(inMemoryFileContent.Content),
            inMemoryFileContent.LastModified, inDefines, libraryDirectories);
    }

    internal KickAssemblerParsedSourceFile ParseStream(string fileName, AntlrInputStream inputStream,
        DateTimeOffset lastModified,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories)
    {
        _logger.LogInformation("Parsing file {FileName}", fileName);
        var lexer = new KickAssemblerLexer(inputStream)
        {
            DefinedSymbols = inDefines.ToHashSet(),
        };
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokenStream)
        {
            BuildParseTree = true
        };
        try
        {
            // InvalidOperationException as a consequence of invalid PopMode can be throws;
            tokenStream.Fill();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed parsing source code for file {FileName} probably because of bad conditional directives #else #endif #elif", fileName);
            return new KickAssemblerParsedSourceFile(fileName, FrozenSet<string>.Empty,
                inDefines, outDefines: inDefines, lastModified, liveContent: null, lexer, tokenStream, parser);
        }
        var tree = parser.program();
        var listener = new KickAssemblerSourceCodeListener();
        ParseTreeWalker.Default.Walk(listener, tree);

        _logger.LogInformation("Parsed file {FileName} has these relative references {References}", fileName,
            listener.ReferencedFiles);
        var absoluteReferencePaths = GetAbsolutePaths(Path.GetDirectoryName(fileName)!, listener.ReferencedFiles, libraryDirectories);
        return new KickAssemblerParsedSourceFile(fileName, absoluteReferencePaths, 
            inDefines, lexer.DefinedSymbols.ToFrozenSet(), lastModified, liveContent: null,
            lexer, tokenStream, parser);
    }

    internal FrozenSet<string> GetAbsolutePaths(string filePath, ImmutableHashSet<string> relativeReferences,
        ImmutableArray<string> libraryDirectories)
    {
        HashSet<string> result = new();
        // makes sure filePath is first directory to look in
        var allDirectories = libraryDirectories.Insert(0, filePath);
        foreach (var reference in relativeReferences)
        {
            var directory = allDirectories.FirstOrDefault(d => _fileService.FileExists(Path.Combine(d, reference)));
            if (directory is not null)
            {
                result.Add(Path.Combine(directory, reference));
            }
            else
            {
                _logger.LogWarning("Could not find referenced source file {File} from file {Source}", reference, filePath);
            }
        }
        return result.ToFrozenSet();
    }
}
