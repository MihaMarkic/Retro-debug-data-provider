namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Link between new type and original from which it was created.
/// </summary>
/// <param name="New"></param>
/// <param name="Original"></param>
/// <typeparam name="TNew"></typeparam>
/// <typeparam name="TOriginal"></typeparam>
public record LinkedOriginal<TNew, TOriginal>(TNew New, TOriginal Original)
    where TNew : notnull
    where TOriginal : notnull;

public static class LinkedOriginalBuilder
{
    public static LinkedOriginal<TNew, TOriginal> Create<TNew, TOriginal>(TNew newItem, TOriginal original)
        where TNew : notnull
        where TOriginal : notnull
        => new (newItem, original);
}