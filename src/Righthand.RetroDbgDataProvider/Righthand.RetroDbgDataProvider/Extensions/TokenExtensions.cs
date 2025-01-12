// ReSharper disable once CheckNamespace

namespace Antlr4.Runtime;

public static class TokenExtensions
{
    public static int Length(this IToken token)
        => token.StopIndex - token.StartIndex + 1;

    public static int EndColumn(this IToken token) => token.Column + token.Length();

    public static bool ContainsColumn(this IToken token, int absoluteColumn)
    {
        return token.StartIndex <= absoluteColumn && token.StopIndex > absoluteColumn;
    }

    public static string TextUpToColumn(this IToken token, int absoluteColumn)
    {
        return token.Text[..absoluteColumn];
    }
}