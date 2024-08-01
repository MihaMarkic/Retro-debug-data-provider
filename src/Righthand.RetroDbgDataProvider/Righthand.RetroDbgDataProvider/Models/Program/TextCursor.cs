namespace Righthand.RetroDbgDataProvider.Models.Program;

public record TextCursor(int Row, int Column)
{
    public static readonly TextCursor Empty = new TextCursor(0, 0);
}