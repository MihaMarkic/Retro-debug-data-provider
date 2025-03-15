namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// A syntax item.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
/// <param name="TokenType"></param>
public record SyntaxItem(int Start, int End, TokenType TokenType)
{
    /// <summary>
    /// Left offset for coloring.
    /// </summary>
    public int LeftMargin { get; init; }

    /// <summary>
    /// Right offset for coloring.
    /// </summary>
    public int RightMargin { get; init; }
    /// <summary>
    /// Gets the length in chars.
    /// </summary>
    public int Length => End-Start+1;
}