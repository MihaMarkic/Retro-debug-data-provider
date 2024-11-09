using System.Collections.Frozen;

namespace Righthand.RetroDbgDataProvider.Models;

public class FilesChangedEventArgs : EventArgs
{
    public new static readonly FilesChangedEventArgs Empty = new FilesChangedEventArgs(
        FrozenSet<string>.Empty,
        FrozenSet<string>.Empty, FrozenSet<string>.Empty);

    public FrozenSet<string> Modified { get; }
    public FrozenSet<string> Deleted { get; }
    public FrozenSet<string> Added { get; }

    public FilesChangedEventArgs(FrozenSet<string> added, FrozenSet<string> modified,
        FrozenSet<string> deleted)
    {
        Modified = modified;
        Deleted = deleted;
        Added = added;
    }
}