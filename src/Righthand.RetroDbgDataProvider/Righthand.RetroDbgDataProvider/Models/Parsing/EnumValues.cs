using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record EnumValues(ImmutableList<EnumValue> Values,  KickAssemblerParser.EnumValuesContext ParserContext)
    : ScopeElement<KickAssemblerParser.EnumValuesContext>(ParserContext);