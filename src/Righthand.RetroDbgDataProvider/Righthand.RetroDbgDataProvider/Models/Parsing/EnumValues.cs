using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents KickAssembler enum values.
/// </summary>
/// <param name="Values"></param>
/// <param name="ParserContext"></param>
public record EnumValues(ImmutableList<EnumValue> Values,  KickAssemblerParser.EnumValuesContext ParserContext)
    : ScopeElement<KickAssemblerParser.EnumValuesContext>(ParserContext);