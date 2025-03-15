using System.Collections.Frozen;
using System.Diagnostics;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Supplies information about file changes.
/// </summary>
public class FilesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets an empty <see cref="FilesChangedEventArgs"/> instance.
    /// </summary>
    public new static readonly FilesChangedEventArgs Empty = new (
        FrozenSet<string>.Empty,
        FrozenSet<string>.Empty, FrozenSet<string>.Empty, CancellationToken.None);

    private readonly List<Task> _clientTasks = [];
    /// <summary>
    /// Gets modified file names.
    /// </summary>
    public FrozenSet<string> Modified { get; }
    /// <summary>
    /// Gets deleted file names.
    /// </summary>
    public FrozenSet<string> Deleted { get; }
    /// <summary>
    /// Gets added file names.
    /// </summary>
    public FrozenSet<string> Added { get; }
    /// <summary>
    /// Each client should insert its async processing here, so that parser knows when all files have been processed.
    /// </summary>
    public CancellationToken CancellationToken { get; }
    /// <summary>
    /// Creates an instance of <see cref="FilesChangedEventArgs"/>.
    /// </summary>
    /// <param name="added"></param>
    /// <param name="modified"></param>
    /// <param name="deleted"></param>
    /// <param name="cancellationToken"></param>
    public FilesChangedEventArgs(FrozenSet<string> added, FrozenSet<string> modified,
        FrozenSet<string> deleted, CancellationToken cancellationToken)
    {
        Modified = modified;
        Deleted = deleted;
        Added = added;
        CancellationToken = cancellationToken;
    }
    /// <summary>
    /// Adds a <see cref="Task"/>.
    /// </summary>
    /// <param name="task"></param>
    public void AddClientTask(Task task)
    {
        _clientTasks.Add(task);
    }
    /// <summary>
    /// Creates a task that will complete when all of the supplied tasks have completed.
    /// </summary>
    /// <returns></returns>
    public Task WaitAllClientTasksAsync()
    {
        Debug.WriteLine($"Awaiting {_clientTasks.Count}");
        return Task.WhenAll(_clientTasks);
    }
}