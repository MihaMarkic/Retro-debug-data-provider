using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

public interface IVariableDefinition
{
    string Name { get; }
}
public abstract record VariableDefinitionBase<T>(string Name, T ParserContext): ScopeElement<T>(ParserContext), IVariableDefinition
    where T : ParserRuleContext;
public record Variable(string Name, KickAssemblerParser.VarContext ParserContext): VariableDefinitionBase<KickAssemblerParser.VarContext>(Name, ParserContext);
public record InitVariable(string Name, KickAssemblerParser.ForVarContext ParserContext): VariableDefinitionBase<KickAssemblerParser.ForVarContext>(Name, ParserContext);