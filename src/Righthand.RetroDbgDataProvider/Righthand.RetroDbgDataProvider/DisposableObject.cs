using System.Text.Json.Serialization;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Base class for disposing.
/// </summary>
public abstract class DisposableObject : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
    }
    [JsonIgnore]
    // ReSharper disable once MemberCanBeProtected.Global
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    protected virtual Task DisposeAsyncCore() => Task.CompletedTask;
}