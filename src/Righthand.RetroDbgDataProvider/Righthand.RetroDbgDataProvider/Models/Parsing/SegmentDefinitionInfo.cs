using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Models;

public record SegmentDefinitionInfo(string Name, int Line, KickAssemblerParser.SegmentDefContext ParserContext)
    : ScopeElement<KickAssemblerParser.SegmentDefContext>(ParserContext);