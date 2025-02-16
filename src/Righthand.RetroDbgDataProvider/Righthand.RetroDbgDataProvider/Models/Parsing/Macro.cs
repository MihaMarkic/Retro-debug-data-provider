using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Macro(string Name, bool IsScopeEsc, ImmutableList<string> Arguments, KickAssemblerParser.MacroDefineContext ParserContext)
    : ScopeElement<KickAssemblerParser.MacroDefineContext>(ParserContext);