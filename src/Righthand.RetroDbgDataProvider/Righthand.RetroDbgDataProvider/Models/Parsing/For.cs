using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler for statement.
/// </summary>
/// <param name="Variables"></param>
/// <param name="ParserContext"></param>
public record For(ImmutableList<InitVariable> Variables, KickAssemblerParser.ForContext ParserContext)
    : ScopeElement<KickAssemblerParser.ForContext>(ParserContext), IScopeRange;