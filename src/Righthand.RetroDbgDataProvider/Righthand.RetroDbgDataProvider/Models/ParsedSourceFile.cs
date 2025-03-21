﻿using System.Collections.Frozen;
using System.Diagnostics;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;
using Label = Righthand.RetroDbgDataProvider.Models.Parsing.Label;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Represents syntax file info.
/// </summary>
/// <param name="SyntaxLines"></param>
/// <param name="IgnoredDefineContent"></param>
/// <param name="SyntaxErrorLines"></param>
/// <param name="AllTokensByLineMap"></param>
public record FileSyntaxInfo(
    FrozenDictionary<int, SyntaxLine> SyntaxLines,
    ImmutableArray<MultiLineTextRange> IgnoredDefineContent,
    FrozenDictionary<int, SyntaxErrorLine> SyntaxErrorLines,
    FrozenDictionary<int, ImmutableArray<IToken>> AllTokensByLineMap);
/// <summary>
/// Represents a parsed source file.
/// </summary>
public interface IParsedSourceFile
{
    /// <summary>
    /// Gets root scope.
    /// </summary>
    Scope DefaultScope { get; }
}
/// <inheritdoc cref="IParsedSourceFile"/>
public abstract class ParsedSourceFile: IParsedSourceFile
{
    /// <summary>
    /// Gets file name.
    /// </summary>
    public string FileName { get; }
    /// <summary>
    /// Relative path to either project or library, depends on file origin
    /// </summary>
    public string RelativePath { get; }
    /// <summary>
    /// Gets a list of referenced files.
    /// </summary>
    public ImmutableArray<ReferencedFileInfo> ReferencedFiles { get; private set; }
    /// <summary>
    /// Preprocessor symbol names defined to include this file.
    /// </summary>
    public FrozenSet<string> InDefines { get; }
    /// <summary>
    /// Preprocessor symbol names defined in this file.
    /// </summary>
    public FrozenSet<string> OutDefines { get; }
    /// <summary>
    /// Gets file's last modified date on file system.
    /// </summary>
    public DateTimeOffset LastModified { get; }
    /// <inheritdoc />
    public Scope DefaultScope { get; }
    /// <summary>
    /// Unsaved live content.
    /// </summary>
    public string? LiveContent { get; }
    /// <summary>
    /// All tokens regardless of channel.
    /// </summary>
    public ImmutableArray<IToken> AllTokens { get; }
    /// <summary>
    /// All tokens from default channel.
    /// </summary>
    public ImmutableArray<IToken> Tokens { get; private set; }
    /// <summary>
    /// All tokens regardless of channel mapped by 0 based line index.
    /// </summary>
    public FrozenDictionary<int, ImmutableArray<IToken>> AllTokensByLineMap { get; private set; }

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
    /// <summary>
    /// Get all lines containing syntax tokens.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    protected abstract Task<FrozenDictionary<int, SyntaxLine>> GetSyntaxLinesAsync(CancellationToken ct);
    private Task<FileSyntaxInfo>? _syntaxInfoInitTask;
    /// <summary>
    /// Gets all syntax errors.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    protected abstract FrozenDictionary<int, SyntaxErrorLine> GetSyntaxErrors(CancellationToken ct);
    private FrozenDictionary<int, SyntaxErrorLine>? _syntaxErrors;
    /// <summary>
    /// Creates an instance of <see cref="ParsedSourceFile"/>.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="relativePath"></param>
    /// <param name="allTokens"></param>
    /// <param name="referencedFiles"></param>
    /// <param name="inDefines"></param>
    /// <param name="outDefines"></param>
    /// <param name="defaultScope"></param>
    /// <param name="lastModified"></param>
    /// <param name="liveContent"></param>
    protected ParsedSourceFile(string fileName, string relativePath, ImmutableArray<IToken> allTokens, ImmutableArray<ReferencedFileInfo> referencedFiles, FrozenSet<string> inDefines,
        FrozenSet<string> outDefines, 
        Scope defaultScope,
        DateTimeOffset lastModified, string? liveContent)
    {
        FileName = fileName;
        RelativePath = relativePath;
        AllTokens = allTokens;
        ReferencedFiles = referencedFiles;
        InDefines = inDefines;
        OutDefines = outDefines;
        DefaultScope = defaultScope;
        LastModified = lastModified;
        LiveContent = liveContent;
        // these two properties below are populated asynchronously through GetSyntaxInfoAsync function
        Tokens = ImmutableArray<IToken>.Empty;
        AllTokensByLineMap = FrozenDictionary<int, ImmutableArray<IToken>>.Empty;
    }

    // /// <summary>
    // /// Checks if there are completion options available at document offset defined by <param name="offset"/>.
    // /// </summary>
    // /// <param name="trigger">Trigger type that invoked completion check</param>
    // /// <param name="offset">Offset from text start</param>
    // /// <returns>Returns possible completion options, null otherwise</returns>
    // public virtual CompletionOption? GetCompletionOption(TextChangeTrigger trigger, int offset) => null;

    /// <summary>
    /// Checks if there are completion options available at document position defined by <paramref name="line"/> and <paramref name="column"/>.
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="triggerChar"></param>
    /// <param name="line">0 based line index</param>
    /// <param name="column">0 based column index</param>
    /// <param name="text">Source text</param>
    /// <param name="textStart">Start index for text</param>
    /// <param name="textLength">Text length</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual CompletionOption? GetCompletionOption(TextChangeTrigger trigger, TriggerChar triggerChar, int line, int column,
        string text, int textStart, int textLength, CompletionOptionContext context) => null;

    /// <summary>
    /// Returns all tokens regardless of channels.
    /// </summary>
    /// <returns>A tuple consisting of all tokens and all tokens mapped by 0 based line index.</returns>
    protected virtual FrozenDictionary<int, ImmutableArray<IToken>> GetAllTokensPerLine()
    {
        return FrozenDictionary<int, ImmutableArray<IToken>>.Empty;
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

            (_syntaxLines, var ignoredDefineContent, _syntaxErrors, AllTokensByLineMap) =
                await _syntaxInfoInitTask;
            Tokens = [..AllTokens.Where(t => t.Channel == 0)];
            // since assigning a non-nullable value to nullable field results in warning, I'll do it through a variable instead
            _ignoredDefineContent = ignoredDefineContent;
            Debug.WriteLine("Parsing done");
        }

        return new FileSyntaxInfo(_syntaxLines, _ignoredDefineContent.Value,
            _syntaxErrors ?? FrozenDictionary<int, SyntaxErrorLine>.Empty, AllTokensByLineMap);
    }

    private async Task<FileSyntaxInfo> InitForSyntaxInfoCoreAsync(CancellationToken ct)
    {
        var initializeForSyntaxParsingTask = Task.Run(GetAllTokensPerLine, ct).ConfigureAwait(false);
        var syntaxLinesTask = Task.Run(async () => await GetSyntaxLinesAsync(ct).ConfigureAwait(false), ct)
            .ConfigureAwait(false);
        var ignoredDefineContent = await Task.Run(() => GetIgnoredDefineContent(ct), ct).ConfigureAwait(false);
        var syntaxErrorsTask = Task.Run(() => GetSyntaxErrors(ct), ct).ConfigureAwait(false);
        var syntaxErrors = await syntaxErrorsTask;
        var syntaxLines = await syntaxLinesTask;
        var allTokensByLineMap = await initializeForSyntaxParsingTask;
        return new FileSyntaxInfo(syntaxLines, ignoredDefineContent,
            syntaxErrors, allTokensByLineMap);
    }

    /// <summary>
    /// Updates referenced file info.
    /// </summary>
    /// <param name="old"></param>
    /// <param name="new"></param>
    public void UpdateReferencedFileInfo(ReferencedFileInfo old, ReferencedFileInfo @new)
    {
        ReferencedFiles = ReferencedFiles.Replace(old, @new);
    }
    /// <summary>
    /// Gets token range at give position.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public abstract SingleLineTextRange? GetTokenRangeAt(int line, int column);

    /// <summary>
    /// Checks whether given <paramref name="line"/> falls into ignored content.
    /// </summary>
    /// <param name="line"></param>
    /// <returns>True when <paramref name="line"/> is within ignored content, false otherwise.</returns>
    protected bool IsLineIgnoredContent(int line)
    {
        return _ignoredDefineContent?.Any(r => r.Start.Row <= line && r.End.Row >= line) ?? false;
    }
}