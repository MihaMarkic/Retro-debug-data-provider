using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler constant.
/// </summary>
/// <param name="Name"></param>
/// <param name="Value"></param>
/// <param name="ParserContext"></param>
public record Constant(string Name, string Value,  KickAssemblerParser.ConstContext ParserContext): ScopeElement<KickAssemblerParser.ConstContext>(ParserContext);