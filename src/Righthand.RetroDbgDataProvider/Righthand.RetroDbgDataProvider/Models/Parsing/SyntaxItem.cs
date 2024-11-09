namespace Righthand.RetroDbgDataProvider.Models.Parsing;

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
}
public record FileReferenceSyntaxItem(int Start, int End, string Path): SyntaxItem(Start, End, TokenType.FileReference);