// ReSharper disable once CheckNamespace

using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Antlr4.Runtime;

/// <summary>
/// Provides extensions to <see cref="IToken"/>.
/// </summary>
public static class TokenExtensions
{
    /// <summary>
    /// Gets the lenght of token text.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static int Length(this IToken token)
        => token.StopIndex - token.StartIndex + 1;
    /// <summary>
    /// Gets end column of text.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static int EndColumn(this IToken token) => token.Column + token.Length();
    /// <summary>
    /// Evaluates whether token contains given position in text with exclusive edges.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="absoluteColumn"></param>
    /// <returns></returns>
    public static bool ContainsColumn(this IToken token, int absoluteColumn)
    {
        return token.StartIndex < absoluteColumn && token.StopIndex >= absoluteColumn - 1;
    }
    /// <summary>
    /// Evaluates whether token contains given position in text with inclusive edges.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="absoluteColumn"></param>
    /// <returns></returns>
    public static bool ContainsColumnWithInclusiveEdge(this IToken token, int absoluteColumn)
    {
        return token.StartIndex <= absoluteColumn && token.StopIndex >= absoluteColumn - 1;
    }
    /// <summary>
    /// Gets text up to give column.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="absoluteColumn"></param>
    /// <returns></returns>
    public static string TextUpToColumn(this IToken token, int absoluteColumn)
    {
        var endIndex = absoluteColumn - token.StartIndex;
        return token.Text[..endIndex];
    }
    /// <summary>
    /// Gets <see cref="RangeInFile"/> for given context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static RangeInFile ToRange(this ParserRuleContext context)
    {
        return new RangeInFile(
            context.Start.ToPositionAtStart(),
            context.Stop.ToPositionAtEnd()
        );
    }
    /// <summary>
    /// Gets position of the start of the token text.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Position? ToPositionAtStart(this IToken? token)
    {
        if (token is null)
        {
            return null;
        }
        return new (token.Line-1, token.Column, token);
    }
    /// <summary>
    /// Gets the position of the end of the token text.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Position? ToPositionAtEnd(this IToken? token)
    {
        if (token is null)
        {
            return null;
        }
        return new (token.Line-1, token.Column + token.Length(), token);
    }
}