namespace Righthand.RetroDbgDataProvider.Models.Program;

public record TextRange(TextCursor Start, TextCursor End)
{
    public static readonly TextRange Empty = new TextRange(TextCursor.Empty, TextCursor.Empty);
    public bool Contains(TextCursor cursor)
    {
        return
            (cursor.Row == Start.Row && cursor.Column >= Start.Column || cursor.Row > Start.Row)
            && (cursor.Row == End.Row && cursor.Column <= End.Column || cursor.Row < End.Row);
    }
}

public sealed record DesignSingleLineTextRange(): TextRange(new TextCursor(1, 5), new TextCursor(5, 5));
public sealed record DesignMultiLineTextRange(): TextRange(new TextCursor(1, 5), new TextCursor(3, 4));