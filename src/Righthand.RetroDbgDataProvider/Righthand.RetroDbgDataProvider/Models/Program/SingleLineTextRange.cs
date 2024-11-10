namespace Righthand.RetroDbgDataProvider.Models.Program;


/// <summary>
/// Represents open ended text range in single line.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
public record SingleLineTextRange(int? Start, int? End)
{
    public bool IsClosed => Start.HasValue && End.HasValue;
    public int Length => End!.Value - Start!.Value;
}