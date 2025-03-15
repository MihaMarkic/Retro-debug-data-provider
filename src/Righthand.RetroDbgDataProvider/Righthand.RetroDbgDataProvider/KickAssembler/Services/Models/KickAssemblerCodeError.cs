namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;
/// <summary>
/// Base class for code errors.
/// </summary>
public abstract record KickAssemblerCodeError
{
    /// <summary>
    /// Gets 0(?) based line index. 
    /// </summary>
    public abstract int Line { get; }
    /// <summary>
    /// Gets position in the line.
    /// </summary>
    public abstract int CharPositionInLine { get; }
    /// <summary>
    /// Gets error message.
    /// </summary>
    public abstract string Message { get; }
}