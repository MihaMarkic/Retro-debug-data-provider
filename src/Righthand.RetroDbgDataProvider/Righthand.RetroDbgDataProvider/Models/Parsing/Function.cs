using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler function.
/// </summary>
/// <param name="Name"></param>
/// <param name="IsScopeEsc"></param>
/// <param name="Arguments"></param>
/// <param name="ParserContext"></param>
public record Function(string Name, bool IsScopeEsc, ImmutableList<string> Arguments,  KickAssemblerParser.FunctionDefineContext ParserContext)
    : ScopeElement<KickAssemblerParser.FunctionDefineContext>(ParserContext);