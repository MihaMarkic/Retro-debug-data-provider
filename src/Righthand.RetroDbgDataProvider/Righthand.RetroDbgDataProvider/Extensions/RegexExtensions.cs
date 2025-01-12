using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

public static class RegexExtensions
{
    public static bool IsWithin([NotNullWhen(true)]this Group? group, int cursor)
    {
        if (group is null)
        {
            return false;
        }
        return group.Index <= cursor && group.Index+group.Length > cursor;
    }
}