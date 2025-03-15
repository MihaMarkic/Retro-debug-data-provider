namespace Righthand.RetroDbgDataProvider.Models.Program;


/// <summary>
/// Represents open ended text range in single line.
/// </summary>
/// <param name="Start"></param>
/// <param name="End"></param>
public record SingleLineTextRange(int? Start, int? End)
{
    /// <summary>
    /// Gets a value that indicates whether range is open or closed.
    /// </summary>
    public bool IsClosed => Start.HasValue && End.HasValue;
    /// <summary>
    /// Gets the length of the range.
    /// </summary>
    /// <remarks>Valid only for <see cref="IsClosed"/> ranges.</remarks>
    public int Length => End!.Value - Start!.Value;
}