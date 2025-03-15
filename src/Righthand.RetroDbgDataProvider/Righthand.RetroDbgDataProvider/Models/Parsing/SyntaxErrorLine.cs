namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// An immutable list of <see cref="SyntaxError"/> errors in the line.
/// </summary>
/// <param name="Items"></param>
public record SyntaxErrorLine(ImmutableArray<SyntaxError> Items)
{
    /// <summary>
    /// Gets a line without errors.
    /// </summary>
    public static SyntaxErrorLine Empty = new(ImmutableArray<SyntaxError>.Empty);
    /// <summary>
    /// Adds an error item a returns a new instance.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public SyntaxErrorLine Add(SyntaxError item)
    {
        return this with
        {
            Items = Items.Add(item),
        };
    }
}