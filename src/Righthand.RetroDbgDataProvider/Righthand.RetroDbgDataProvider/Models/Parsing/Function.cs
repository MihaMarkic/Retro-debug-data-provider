using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Function(string Name, bool IsScopeEsc, ImmutableList<string> Arguments,  KickAssemblerParser.FunctionDefineContext ParserContext)
    : ScopeElement<KickAssemblerParser.FunctionDefineContext>(ParserContext);