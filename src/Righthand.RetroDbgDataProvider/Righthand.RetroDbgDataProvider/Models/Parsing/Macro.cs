using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler macro.
/// </summary>
/// <param name="Name"></param>
/// <param name="IsScopeEsc"></param>
/// <param name="Arguments"></param>
/// <param name="ParserContext"></param>
public record Macro(string Name, bool IsScopeEsc, ImmutableList<string> Arguments, KickAssemblerParser.MacroDefineContext ParserContext)
    : ScopeElement<KickAssemblerParser.MacroDefineContext>(ParserContext);