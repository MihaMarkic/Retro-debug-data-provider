using Antlr4.Runtime;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Open-ended position within file.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
public readonly record struct RangeInFile(Position? Start, Position? End)
{
    /// <summary>
    /// Evaluates whether range contains given position.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public bool IsInRange(int line, int column)
    {
        if (Start is not null)
        {
            if (line < Start.Value.Line || line == Start.Value.Line && column < Start.Value.Column)
            {
                return false;
            }
        }

        if (End is not null)
        {
            if (line > End.Value.Line || line == End.Value.Line && column > End.Value.Column)
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
/// <param name="Token"></param>
public readonly record struct Position(int Line, int Column, IToken Token);