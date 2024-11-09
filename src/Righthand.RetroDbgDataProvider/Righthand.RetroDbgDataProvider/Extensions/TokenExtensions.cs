// ReSharper disable once CheckNamespace

namespace Antlr4.Runtime;

public static class TokenExtensions
{
    public static int Length(this IToken token)
        => token.StopIndex - token.StartIndex + 1;

    public static int EndColumn(this IToken token) => token.Column + token.Length();
}