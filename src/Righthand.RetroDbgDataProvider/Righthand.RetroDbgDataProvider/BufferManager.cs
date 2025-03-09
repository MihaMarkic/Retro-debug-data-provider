using System.Buffers;

namespace Righthand.RetroDbgDataProvider;

/// <summary>
/// Manages byte array buffers from pool.
/// </summary>
/// <threadsafe>Class is thread safe.</threadsafe>
/// <remarks>Taken from ViceBridge</remarks>
public static class BufferManager
{
    /// <summary>
    /// Gets a buffer with given size or larger.
    /// </summary>
    /// <param name="minLength">A minimum size requested.</param>
    /// <returns>An instance of <see cref="ManagedBuffer{T}"/> with a byte buffer of given minimal size or larger.</returns>
    public static ManagedBuffer<T> GetBuffer<T>(uint minLength)
    {
        return GetBuffer(ArrayPool<T>.Shared, minLength);
    }

    /// <summary>
    /// Gets a buffer with given size or larger.
    /// </summary>
    /// <param name="pool">Pool where the buffer is retrieved from.</param>
    /// <param name="minLength">A minimum size requested.</param>
    /// <returns>An instance of <see cref="ManagedBuffer{T}"/> with a byte buffer of given minimal size or larger.</returns>
    public static ManagedBuffer<T> GetBuffer<T>(this ArrayPool<T> pool, uint minLength)
    {
        return new ManagedBuffer<T>(pool, pool.Rent((int)minLength), minLength);
    }
}

/// <summary>
/// Holds byte array retrieved form a pool.
/// </summary>
/// <remarks>
/// It should be disposed once it is not need anymore to return data to the pool. Otherwise, memory leaks will happen.
/// </remarks>
public readonly struct ManagedBuffer<T> : IDisposable
{
    /// <summary>
    /// An empty buffer.
    /// </summary>
    public static readonly ManagedBuffer<T> Empty = new(0);

    /// <summary>
    /// Byte array of minimal size of <see cref="Size"/>.
    /// </summary>
    public T[] Data { get; }

    private readonly ArrayPool<T>? _pool;

    /// <summary>
    /// The requested size of data.
    /// </summary>
    /// <remarks>Depending on the pool, the actual <see cref="Data"/> length can be larger.</remarks>
    public uint Size { get; }

    ManagedBuffer(uint size)
    {
        _pool = null;
        Data = [];
        Size = size;
    }

    internal ManagedBuffer(ArrayPool<T> pool, T[] data, uint size)
    {
        this._pool = pool;
        Data = data;
        Size = size;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ManagedBuffer{T}"/>.
    /// </summary>
    public void Dispose()
    {
        _pool?.Return(Data);
    }
}