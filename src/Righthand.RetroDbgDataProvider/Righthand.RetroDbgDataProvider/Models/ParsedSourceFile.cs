using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models;

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
    public abstract Lazy<ImmutableArray<TextRange>> IgnoredDefineContent { get; }

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

    public void UpdateReferencedFileInfo(ReferencedFileInfo old, ReferencedFileInfo @new)
    {
        ReferencedFiles = ReferencedFiles.Replace(old, @new);
    }
}