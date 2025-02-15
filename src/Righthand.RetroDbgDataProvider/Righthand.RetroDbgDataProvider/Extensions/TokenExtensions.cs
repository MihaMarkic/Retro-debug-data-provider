// ReSharper disable once CheckNamespace

using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Antlr4.Runtime;

public static class TokenExtensions
{
    public static int Length(this IToken token)
        => token.StopIndex - token.StartIndex + 1;

    public static int EndColumn(this IToken token) => token.Column + token.Length();

    public static bool ContainsColumn(this IToken token, int absoluteColumn)
    {
        return token.StartIndex < absoluteColumn && token.StopIndex >= absoluteColumn - 1;
    }

    public static bool ContainsColumnWithInclusiveEdge(this IToken token, int absoluteColumn)
    {
        return token.StartIndex <= absoluteColumn && token.StopIndex >= absoluteColumn - 1;
    }

    public static string TextUpToColumn(this IToken token, int absoluteColumn)
    {
        var endIndex = absoluteColumn - token.StartIndex;
        return token.Text[..endIndex];
    }

    public static Position? ToPositionAtStart(this IToken? token)
    {
        if (token is null)
        {
            return null;
        }
        return new (token.Line-1, token.Column, token.TokenIndex);
    }
    public static Position? ToPositionAtEnd(this IToken? token)
    {
        if (token is null)
        {
            return null;
        }
        return new (token.Line-1, token.Column + token.Length(), token.TokenIndex);
    }
}