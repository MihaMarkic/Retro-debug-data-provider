using System.Collections.Frozen;
using System.Diagnostics;

namespace Righthand.RetroDbgDataProvider.Models;

public class FilesChangedEventArgs : EventArgs
{
    public new static readonly FilesChangedEventArgs Empty = new FilesChangedEventArgs(
        FrozenSet<string>.Empty,
        FrozenSet<string>.Empty, FrozenSet<string>.Empty, CancellationToken.None);

    private List<Task> _clientTasks = new List<Task>();
    public FrozenSet<string> Modified { get; }
    public FrozenSet<string> Deleted { get; }
    public FrozenSet<string> Added { get; }
    /// <summary>
    /// Each client should insert its async processing here, so that parser knows when all files have been processed.
    /// </summary>
    public CancellationToken CancellationToken { get; }
    public FilesChangedEventArgs(FrozenSet<string> added, FrozenSet<string> modified,
        FrozenSet<string> deleted, CancellationToken cancellationToken)
    {
        Modified = modified;
        Deleted = deleted;
        Added = added;
        CancellationToken = cancellationToken;
    }
    public void AddClientTask(Task task)
    {
        _clientTasks.Add(task);
    }
    public Task WaitAllClientTasksAsync()
    {
        Debug.WriteLine($"Awaiting {_clientTasks.Count}");
        return Task.WhenAll(_clientTasks);
    }
}