using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;
/// <summary>
/// Provides parsing of the KickAssembler project's source code.
/// </summary>
public class KickAssemblerSourceCodeParser: ISourcecodeParser
{
    private readonly ILogger<KickAssemblerSourceCodeParser> _logger;
    private readonly IFileService _fileService;
    private readonly KickAssemblerPreprocessor _preprocessor;
    private CancellationTokenSource? _parsingCts;
    // file with files that reference it
    private ImmutableDictionary<string, ParsedSourceFile> _allFiles;
    private string? _projectDirectory;
    /// <summary>
    /// Creates a new instance of <see cref="KickAssemblerSourceCodeParser"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileService"></param>
    public KickAssemblerSourceCodeParser(ILogger<KickAssemblerSourceCodeParser> logger, IFileService fileService,
        KickAssemblerPreprocessor preprocessor)
    {
        _logger = logger;
        _fileService = fileService;
        _preprocessor = preprocessor;
        _allFiles = ImmutableDictionary<string, ParsedSourceFile>.Empty;
    }
    /// <inheritdoc cref="ISourcecodeParser"/>
    public Task InitialParseAsync(string projectDirectory, CancellationToken ct)
    {
        if (_projectDirectory is not null)
        {
            throw new Exception("Already initialized");
        }
        _projectDirectory = projectDirectory;
        _parsingCts?.Cancel();
        _parsingCts = new();

        return Task.CompletedTask;
    }
    /// <inheritdoc cref="ISourcecodeParser"/>
    public Task ParseAsync(ImmutableArray<string> changedFiles, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private Task<ImmutableDictionary<string, ParsedSourceFile>> GetAllFilesAsync(string startFile, CancellationToken ct)
    {
        try
        {
            if (!_fileService.FileExists(startFile))
            {
                _logger.LogWarning("Can't find start file {StartFile} in project's directory {Directory}", startFile, _projectDirectory);
                return Task.FromResult(ImmutableDictionary<string, ParsedSourceFile>.Empty);
            }

            var allFiles = new Dictionary<string, ParsedSourceFile>();
            return Task.FromResult(allFiles.ToImmutableDictionary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed enumerating project files");
            throw;
        }
    }

    internal ParsedSourceFile ParseFile(string fileName, FrozenSet<string> inDefines, ImmutableArray<string> libraryDirectories)
    {
        var lastWrite = _fileService.GetLastWriteTime(fileName);
        using (var content = _fileService.OpenRead(fileName))
        {
            return ParseStream(fileName, content, lastWrite, inDefines, libraryDirectories);
        }
    }

    internal ParsedSourceFile ParseStream(string fileName, Stream content, DateTimeOffset lastModified,
        FrozenSet<string> inDefines,
        ImmutableArray<string> libraryDirectories)
    {
        _logger.LogInformation("Parsing file {FileName}", fileName);
        var input = new AntlrInputStream(content);
        var lexer = new KickAssemblerLexer(input);
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();
        _preprocessor.FilterUndefined(tokenStream, inDefines);
        var parser = new KickAssemblerParser(tokenStream)
        {
            BuildParseTree = true
        };
        var tree = parser.program();
        var listener = new KickAssemblerSourceCodeListener();
        ParseTreeWalker.Default.Walk(listener, tree);

        var tokens = tokenStream.GetTokens();

        _logger.LogInformation("Parsed file {FileName} has these relative references {References}", fileName,
            listener.ReferencedFiles);
        var absoluteReferencePaths = GetAbsolutePaths(Path.GetDirectoryName(fileName)!, listener.ReferencedFiles, libraryDirectories);
        return new ParsedSourceFile(fileName, absoluteReferencePaths, 
            InDefines:FrozenSet<string>.Empty, OutDefines:FrozenSet<string>.Empty, LastModified: lastModified, LiveContent: null);
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
                _logger.LogWarning("Could not find referenced source file {File}", reference);
            }
        }
        return result.ToFrozenSet();
    }
    
    //
    // internal static ImmutableHashSet<string> ExtractReferencesFiles(IList<IToken> tokens)
    // {
    //     List<string> referencedFiles = new();
    //     // skip first and last because import(if) has to be prefixed with # and ends with filename
    //     for (int i = 1; i < tokens.Count - 1; i++)
    //     {
    //         switch (tokens[i].Type)
    //         {
    //             case KickAssemblerLexer.IMPORT:
    //                 if (tokens[i - 1].Type == KickAssemblerLexer.HASH && tokens[i + 1].Type == KickAssemblerLexer.STRING)
    //                 {
    //                     referencedFiles.Add(tokens[i + 1].Text.Trim('"'));
    //                 }
    //
    //                 break;
    //             case KickAssemblerLexer.IMPORTIF:
    //                 if (tokens[i - 1].Type == KickAssemblerLexer.HASH && MatchTokens(tokens, i + 1,
    //                         KickAssemblerLexer.UNQUOTED_STRING, KickAssemblerLexer.DOUBLE_QUOTE,
    //                         KickAssemblerLexer.STRING, KickAssemblerLexer.DOUBLE_QUOTE))
    //                 {
    //                     referencedFiles.Add(tokens[i + 3].Text);
    //                 }
    //
    //                 break;
    //         }
    //     }
    //     return [..referencedFiles];
    // }
    //
    // /// <summary>
    // /// Compares subrange with expected tokens.
    // /// </summary>
    // /// <param name="tokens"></param>
    // /// <param name="startIndex"></param>
    // /// <param name="expectedTokens"></param>
    // /// <returns></returns>
    // private static bool MatchTokens(IList<IToken> tokens, int startIndex, params int[] expectedTokens)
    // {
    //     if (startIndex + expectedTokens.Length > tokens.Count)
    //     {
    //         return false;
    //     }
    //
    //     for (int i = 0; i < expectedTokens.Length; i++)
    //     {
    //         if (tokens[i].Type != expectedTokens[i])
    //         {
    //             return false;
    //         }
    //     }
    //
    //     return true;
    // }
}
