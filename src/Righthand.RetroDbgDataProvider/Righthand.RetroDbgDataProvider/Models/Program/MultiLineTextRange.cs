namespace Righthand.RetroDbgDataProvider.Models.Program;

/// <summary>
/// Text range in string.
/// </summary>
/// <param name="Start">Start location</param>
/// <param name="End">End location</param>
public record MultiLineTextRange(TextCursor Start, TextCursor End)
{
    /// <summary>
    /// Empty value.
    /// </summary>
    public static readonly MultiLineTextRange Empty = new MultiLineTextRange(TextCursor.Empty, TextCursor.Empty);
    /// <summary>
    /// Checks whether range contains <paramref name="cursor"/>.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    public bool Contains(TextCursor cursor)
    {
        return
            (cursor.Row == Start.Row && cursor.Column >= Start.Column || cursor.Row > Start.Row)
            && (cursor.Row == End.Row && cursor.Column <= End.Column || cursor.Row < End.Row);
    }
}
/// <summary>
/// Design time type with a single line range.
/// </summary>
public sealed record DesignSingleLineMultiLineTextRange(): MultiLineTextRange(new TextCursor(1, 5), new TextCursor(5, 5));
/// <summary>
/// Design time type with a multiline line range.
/// </summary>
public sealed record DesignMultiLineMultiLineTextRange(): MultiLineTextRange(new TextCursor(1, 5), new TextCursor(3, 4));