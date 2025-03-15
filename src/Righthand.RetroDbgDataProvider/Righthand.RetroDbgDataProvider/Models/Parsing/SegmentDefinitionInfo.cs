using Righthand.RetroDbgDataProvider.KickAssembler;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.Models;

/// <summary>
/// Defines a KickAssembler segment.
/// </summary>
/// <param name="Name"></param>
/// <param name="Line"></param>
/// <param name="ParserContext"></param>
public record SegmentDefinitionInfo(string Name, int Line, KickAssemblerParser.SegmentDefContext ParserContext)
    : ScopeElement<KickAssemblerParser.SegmentDefContext>(ParserContext);