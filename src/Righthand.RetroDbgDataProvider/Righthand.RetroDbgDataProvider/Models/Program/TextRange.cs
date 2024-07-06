namespace Righthand.RetroDbgDataProvider.Models.Program;

public record TextRange(TextCursor Start, TextCursor End)
{
    public bool Contains(TextCursor cursor)
    {
        return
            (cursor.Row == Start.Row && cursor.Column >= Start.Column || cursor.Row > Start.Row)
            && (cursor.Row == End.Row && cursor.Column <= End.Column || cursor.Row < End.Row);
    }
}