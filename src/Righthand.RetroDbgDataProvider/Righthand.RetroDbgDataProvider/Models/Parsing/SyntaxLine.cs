namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// A list of <see cref="SyntaxItem"/> representing a line.
/// </summary>
/// <param name="Items"></param>
public record SyntaxLine(ImmutableArray<SyntaxItem> Items);