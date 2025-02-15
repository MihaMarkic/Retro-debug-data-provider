namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Open-ended position within file.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
public record RangeInFile(Position? Start, Position? End)
{
    public bool IsInRange(int line, int column)
    {
        if (Start is not null)
        {
            if (line < Start.Line || line == Start.Line && column < Start.Column)
            {
                return false;
            }
        }

        if (End is not null)
        {
            if (line > End.Line || line == End.Line && column > End.Column)
            {
                return false;
            }
        }
        return true;
    }
}
/// <summary>
/// Position within a file.
/// </summary>
/// <param name="Line">0 based line index</param>
/// <param name="Column">0 based columns index</param>
public record Position(int Line, int Column, int TokenIndex);