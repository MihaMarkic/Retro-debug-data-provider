namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;
public abstract record KickAssemblerCodeError
{
    public abstract int Line { get; }
    public abstract int CharPositionInLine { get; }
    public abstract string Message { get; }
}