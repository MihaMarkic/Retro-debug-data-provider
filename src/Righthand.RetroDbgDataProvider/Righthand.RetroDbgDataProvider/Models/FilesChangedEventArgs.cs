using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

public class FilesChangedEventArgs<T> : EventArgs
    where T : ParsedSourceFile
{
    public new static readonly FilesChangedEventArgs<T> Empty = new FilesChangedEventArgs<T>(
        FrozenDictionary<string, T>.Empty,
        FrozenDictionary<string, T>.Empty, FrozenDictionary<string, T>.Empty);

    public FrozenDictionary<string, T> Modified { get; }
    public FrozenDictionary<string, T> Deleted { get; }
    public FrozenDictionary<string, T> New { get; }

    public FilesChangedEventArgs(FrozenDictionary<string, T> modified,
        FrozenDictionary<string, T> deleted, FrozenDictionary<string, T> @new)
    {
        Modified = modified;
        Deleted = deleted;
        New = @new;
    }
}

public class FilesChangedEventArgs : EventArgs
{
    public new static readonly FilesChangedEventArgs Empty = new FilesChangedEventArgs(
        FrozenDictionary<string, ParsedSourceFile>.Empty,
        FrozenDictionary<string, ParsedSourceFile>.Empty, FrozenDictionary<string, ParsedSourceFile>.Empty);

    public FrozenDictionary<string, ParsedSourceFile> Modified { get; }
    public FrozenDictionary<string, ParsedSourceFile> Deleted { get; }
    public FrozenDictionary<string, ParsedSourceFile> New { get; }

    public FilesChangedEventArgs(FrozenDictionary<string, ParsedSourceFile> modified,
        FrozenDictionary<string, ParsedSourceFile> deleted, FrozenDictionary<string, ParsedSourceFile> @new)
    {
        Modified = modified;
        Deleted = deleted;
        New = @new;
    }
}