namespace Righthand.RetroDbgDataProvider.Comparers;

/// <summary>
/// Compares two instances of <see cref="ISet{T}"/> by content.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SetEqualityComparer<T> : IEqualityComparer<ISet<T>>
{
    public static readonly SetEqualityComparer<T> Default = new();

    public bool Equals(ISet<T>? x, ISet<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.SetEquals(y);
    }

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