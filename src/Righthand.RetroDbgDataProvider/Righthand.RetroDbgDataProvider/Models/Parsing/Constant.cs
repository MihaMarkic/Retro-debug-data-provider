using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public record Constant(string Name, string Value,  KickAssemblerParser.ConstContext ParserContext): ScopeElement<KickAssemblerParser.ConstContext>(ParserContext);