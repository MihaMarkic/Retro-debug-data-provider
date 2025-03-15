using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler label.
/// </summary>
/// <param name="Name"></param>
/// <param name="IsMultiOccurrence"></param>
/// <param name="ParserContext"></param>
public record Label(string Name, bool IsMultiOccurrence,  KickAssemblerParser.LabelNameContext ParserContext)
    : ScopeElement<KickAssemblerParser.LabelNameContext>(ParserContext)
{
    /// <summary>
    /// Gets full label name.
    /// </summary>
    public string FullName => IsMultiOccurrence ? $"!{Name}" : Name;
}