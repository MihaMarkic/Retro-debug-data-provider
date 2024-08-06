namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Link between new type and original from which it was created.
/// </summary>
/// <param name="New"></param>
/// <param name="Original"></param>
/// <typeparam name="TNew"></typeparam>
/// <typeparam name="TOriginal"></typeparam>
internal record LinkedOriginal<TNew, TOriginal>(TNew New, TOriginal Original)
    where TNew : notnull
    where TOriginal : notnull;
/// <summary>
/// Builder for <see cref="LinkedOriginal{TNew, TOriginal}"/>.
/// </summary>
internal static class LinkedOriginalBuilder
{
    /// <summary>
    /// Creates an instance of <see cref="LinkedOriginal{TNew, TOriginal}"/>.
    /// </summary>
    /// <typeparam name="TNew"></typeparam>
    /// <typeparam name="TOriginal"></typeparam>
    /// <param name="newItem"></param>
    /// <param name="original"></param>
    /// <returns></returns>
    internal static LinkedOriginal<TNew, TOriginal> Create<TNew, TOriginal>(TNew newItem, TOriginal original)
        where TNew : notnull
        where TOriginal : notnull
        => new (newItem, original);
}