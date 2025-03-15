namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;

/// <summary>
/// Represent an error in relaxed parsing syntax.
/// </summary>
/// <param name="Context"></param>
public record KickAssemblerParserSyntaxError(KickAssemblerParser.ErrorSyntaxContext Context): KickAssemblerCodeError
{
    /// <inheritdoc />
    public override int Line => Context.Start.Line;
    /// <inheritdoc />
    public override int CharPositionInLine => Context.Start.Column;
    /// <inheritdoc />
    public override string Message => $"Unexpected text {Context.GetText()}";
}