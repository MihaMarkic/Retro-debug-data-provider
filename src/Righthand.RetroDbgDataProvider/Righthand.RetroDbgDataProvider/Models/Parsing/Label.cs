using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Label(string Name, bool IsMultiOccurrence,  KickAssemblerParser.LabelNameContext ParserContext)
    : ScopeElement<KickAssemblerParser.LabelNameContext>(ParserContext)
{
    public string FullName => IsMultiOccurrence ? $"!{Name}" : Name;
}