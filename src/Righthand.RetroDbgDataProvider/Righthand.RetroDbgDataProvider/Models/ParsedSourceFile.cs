using System.Collections.Frozen;
using System.Diagnostics;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models;

public record FileSyntaxInfo(
    FrozenDictionary<int, SyntaxLine> SyntaxLines,
    ImmutableArray<MultiLineTextRange> IgnoredDefineContent,
    FrozenDictionary<int, SyntaxErrorLine> SyntaxErrorLines);

public abstract class ParsedSourceFile
{
    public string FileName { get; }
    public ImmutableArray<ReferencedFileInfo> ReferencedFiles { get; private set; }
    public FrozenSet<string> InDefines { get; }
    public FrozenSet<string> OutDefines { get; }
    public DateTimeOffset LastModified { get; }
    public string? LiveContent { get; }

    /// <summary>
    /// Contains continuous range of ignored content. 
    /// </summary>
    /// <remarks>This content is produced by #if,#elif and #else preprocessor directives.</remarks>
    private ImmutableArray<MultiLineTextRange>? _ignoredDefineContent;
    /// <summary>
    /// Collects all ignored ranges and merges them if they are continuous.
    /// </summary>
    /// <returns>An array of <see cref="MultiLineTextRange"/> values.</returns>
    internal abstract ImmutableArray<MultiLineTextRange> GetIgnoredDefineContent(CancellationToken ct);
    private FrozenDictionary<int, SyntaxLine>? _syntaxLines;
    protected abstract Task<FrozenDictionary<int, SyntaxLine>> GetSyntaxLinesAsync(CancellationToken ct);
    private Task<FileSyntaxInfo>? _syntaxInfoInitTask;
    protected abstract FrozenDictionary<int, SyntaxErrorLine> GetSyntaxErrors(CancellationToken ct);
    private FrozenDictionary<int, SyntaxErrorLine>? _syntaxErrors;
    
    protected ParsedSourceFile(string fileName, ImmutableArray<ReferencedFileInfo> referencedFiles, FrozenSet<string> inDefines,
        FrozenSet<string> outDefines, DateTimeOffset lastModified, string? liveContent)
    {
        FileName = fileName;
        ReferencedFiles = referencedFiles;
        InDefines = inDefines;
        OutDefines = outDefines;
        LastModified = lastModified;
        LiveContent = liveContent;
    }

    /// <summary>
    /// Returns syntax parsing related data necessary for editor.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<FileSyntaxInfo> GetSyntaxInfoAsync(CancellationToken ct)
    {
        Debug.WriteLine("Starting GetSyntaxInfo");
        if (_syntaxLines is null || _ignoredDefineContent is null)
        {
            // parsing already running, wait for it
            if (_syntaxInfoInitTask is null)
            {
                Debug.WriteLine("New parse");
                _syntaxInfoInitTask = InitForSyntaxInfoCoreAsync(ct);
            }
            else
            {
                Debug.WriteLine("Reusing old task");
            }

            (_syntaxLines, _ignoredDefineContent, _syntaxErrors) = await _syntaxInfoInitTask;
            Debug.WriteLine("Parsing done");
        }

        return new FileSyntaxInfo(_syntaxLines, _ignoredDefineContent.Value, _syntaxErrors);
    }

    private async Task<FileSyntaxInfo> InitForSyntaxInfoCoreAsync(CancellationToken ct)
    {
        var syntaxLinesTask = Task.Run(async () => await GetSyntaxLinesAsync(ct).ConfigureAwait(false), ct)
            .ConfigureAwait(false);
        var ignoredDefineContent = await Task.Run(() => GetIgnoredDefineContent(ct), ct).ConfigureAwait(false);
        var syntaxErrors = await Task.Run(() => GetSyntaxErrors(ct), ct).ConfigureAwait(false);
        var syntaxLines = await syntaxLinesTask;
        return new FileSyntaxInfo(syntaxLines, ignoredDefineContent, syntaxErrors);
    }

    public void UpdateReferencedFileInfo(ReferencedFileInfo old, ReferencedFileInfo @new)
    {
        ReferencedFiles = ReferencedFiles.Replace(old, @new);
    }

    public abstract SingleLineTextRange? GetTokenRangeAt(int line, int column);
}