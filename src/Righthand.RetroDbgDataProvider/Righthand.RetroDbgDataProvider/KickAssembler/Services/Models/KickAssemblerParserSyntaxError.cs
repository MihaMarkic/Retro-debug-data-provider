namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;

/// <summary>
/// Represent an error in relaxed parsing syntax.
/// </summary>
/// <param name="Context"></param>
public record KickAssemblerParserSyntaxError(KickAssemblerParser.ErrorSyntaxContext Context): KickAssemblerCodeError
{
    public override int Line => Context.Start.Line;
    public override int CharPositionInLine => Context.Start.Column;
    public override string Message => $"Unexpected text {Context.GetText()}";
}