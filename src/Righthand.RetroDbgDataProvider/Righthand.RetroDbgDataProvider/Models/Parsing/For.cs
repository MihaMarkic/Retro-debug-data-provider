using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record For(ImmutableList<InitVariable> Variables, KickAssemblerParser.ForContext ParserContext)
    : ScopeElement<KickAssemblerParser.ForContext>(ParserContext), IScopeRange;