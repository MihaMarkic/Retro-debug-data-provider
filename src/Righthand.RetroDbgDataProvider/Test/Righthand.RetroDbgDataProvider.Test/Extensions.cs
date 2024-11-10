using System.Diagnostics.CodeAnalysis;

namespace System;
public static class Extensions
{
    /// <summary>
    /// Compares two <see cref="IList{T}"/> instances for items equality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="comparer"></param>
    /// <returns>True when both have same items, false otherwise.</returns>
    public static bool AreEquals<T>(this IList<T> a, IList<T> b, IEqualityComparer<T>? comparer = null)
    {
        var actualComparer = comparer ?? EqualityComparer<T>.Default;
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!actualComparer.Equals(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Makes new line chars cross-platform.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? FixLineEndings(this string? text)
    {
        return text?.Replace("\r\n", "\n");
    }
}
