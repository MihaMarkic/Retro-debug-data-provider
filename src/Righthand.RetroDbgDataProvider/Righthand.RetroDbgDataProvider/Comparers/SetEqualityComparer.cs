namespace Righthand.RetroDbgDataProvider.Comparers;

/// <summary>
/// Compares two instances of <see cref="ISet{T}"/> by content.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SetEqualityComparer<T> : IEqualityComparer<ISet<T>>
{
    /// <summary>
    /// Default comparer.
    /// </summary>
    public static readonly SetEqualityComparer<T> Default = new();

    /// <inheritdoc />
    public bool Equals(ISet<T>? x, ISet<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.SetEquals(y);
    }

    /// <inheritdoc />
    public int GetHashCode(ISet<T> obj)
    {
        var hc = new HashCode();
        foreach (var o in obj)
        {
            hc.Add(o);
        }

        return hc.ToHashCode();
    }
}

/// <summary>
/// Provides support for calculating hash codes.
/// </summary>
public static class Equatable
{
    /// <summary>
    /// Calculates hash codes for all items in <paramref name="source"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="hc"></param>
    /// <typeparam name="T"></typeparam>
    public static void AddHashCode<T>(this ICollection<T> source, ref HashCode hc)
    {
        foreach (var i in source)
        {
            hc.Add(i);
        }
    }
}