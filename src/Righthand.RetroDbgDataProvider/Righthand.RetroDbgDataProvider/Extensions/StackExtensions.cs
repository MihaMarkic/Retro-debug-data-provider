namespace Righthand.RetroDbgDataProvider.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Stack{T}"/> operations.
/// </summary>
public static class StackExtensions
{
    /// <summary>
    /// Pushes given <paramref name="value"/> or creates a new instance.
    /// </summary>
    /// <param name="stack"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Push<T>(this Stack<T> stack, T? value = null)
        where T: class, new()
    {
        var item = value ?? new ();
        stack.Push(item);
        return item;
    }
}