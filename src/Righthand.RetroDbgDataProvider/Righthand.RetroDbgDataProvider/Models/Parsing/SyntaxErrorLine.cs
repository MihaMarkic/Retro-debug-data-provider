namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record SyntaxErrorLine(ImmutableArray<SyntaxError> Items)
{
    public static SyntaxErrorLine Empty = new(ImmutableArray<SyntaxError>.Empty);

    public SyntaxErrorLine Add(SyntaxError item)
    {
        return this with
        {
            Items = Items.Add(item),
        };
    }
}