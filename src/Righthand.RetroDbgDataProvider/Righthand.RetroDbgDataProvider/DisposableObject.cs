using System.Text.Json.Serialization;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Base class for disposing.
/// </summary>
public abstract class DisposableObject : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// Releases all resources used by object.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
    }
    /// <summary>
    /// Gets a value that indicates whether the proxy was already disposed.
    /// </summary>
    [JsonIgnore]
    // ReSharper disable once MemberCanBeProtected.Global
    public bool IsDisposed => _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws an exception if object has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    /// <returns></returns>
    protected virtual Task DisposeAsyncCore() => Task.CompletedTask;
}