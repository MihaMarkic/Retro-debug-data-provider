namespace Righthand.RetroDbgDataProvider.Models.Program;
/// <summary>
/// Represents a location in text file.
/// </summary>
/// <param name="Row">Row (starts with 1)</param>
/// <param name="Column">Column (starts with 1) in line</param>
public record TextCursor(int Row, int Column)
{
    /// <summary>
    /// Represents a start location.
    /// </summary>
    public static readonly TextCursor Empty = new TextCursor(0, 0);
}