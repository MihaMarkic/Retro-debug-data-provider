using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace Righthand.RetroDbgDataProvider.Models;

public abstract class ParsedSourceFile
{
    public string FileName { get; }
    public FrozenSet<string> ReferencedFiles { get; }
    public FrozenSet<string> InDefines { get; }
    public FrozenSet<string> OutDefines { get; }
    public DateTimeOffset LastModified { get; }
    public string? LiveContent { get; }
    /// <summary>
    /// Contains continuous range of ignored content. 
    /// </summary>
    /// <remarks>This content is produced by #if,#elif and #else preprocessor directives.</remarks>
    public abstract Lazy<ImmutableArray<TextRange>> IgnoredDefineContent { get; }

    protected ParsedSourceFile(string fileName, FrozenSet<string> referencedFiles, FrozenSet<string> inDefines,
        FrozenSet<string> outDefines, DateTimeOffset lastModified, string? liveContent)
    {
        FileName = fileName;
        ReferencedFiles = referencedFiles;
        InDefines = inDefines;
        OutDefines = outDefines;
        LastModified = lastModified;
        LiveContent = liveContent;
    }
}

public interface IParsedFilesIndex<out T>
{
    T? GetValueOrDefault(string name);
    ImmutableArray<string> Keys { get; }
}
public class ParsedFilesIndex<T>: IParsedFilesIndex<T>
    where T : ParsedSourceFile
{
    public static readonly ParsedFilesIndex<T> Empty = new ParsedFilesIndex<T>(FrozenDictionary<string, T>.Empty);
    private readonly FrozenDictionary<string, T> _data;

    public ParsedFilesIndex(FrozenDictionary<string, T> data)
    {
        _data = data;
    }
    public T? GetValueOrDefault(string name) => _data.GetValueOrDefault(name);
    public ImmutableArray<string> Keys => _data.Keys;
}