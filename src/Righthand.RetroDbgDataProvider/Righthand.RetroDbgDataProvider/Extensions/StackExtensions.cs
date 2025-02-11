namespace Righthand.RetroDbgDataProvider.Extensions;

public static class StackExtensions
{
    public static T Push<T>(this Stack<T> stack, T? value = null)
        where T: class, new()
    {
        var item = value ?? new ();
        stack.Push(item);
        return item;
    }
}