using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.KickAssembler;

namespace Righthand.RetroDbgDataProvider.Models.Parsing;

/// <summary>
/// Represents a KickAssembler variable.
/// </summary>
public interface IVariableDefinition
{
    /// <summary>
    /// Gets variabla name.
    /// </summary>
    string Name { get; }
}
/// <summary>
/// Base class for KickAssembler variable definition.
/// </summary>
/// <param name="Name"></param>
/// <param name="ParserContext"></param>
/// <typeparam name="T"></typeparam>
public abstract record VariableDefinitionBase<T>(string Name, T ParserContext): ScopeElement<T>(ParserContext), IVariableDefinition
    where T : ParserRuleContext;
/// <summary>
/// Represents a variable.
/// </summary>
/// <param name="Name"></param>
/// <param name="ParserContext"></param>
public record Variable(string Name, KickAssemblerParser.VarContext ParserContext): VariableDefinitionBase<KickAssemblerParser.VarContext>(Name, ParserContext);
/// <summary>
/// Represents a variable initialization. 
/// </summary>
/// <param name="Name"></param>
/// <param name="ParserContext"></param>
public record InitVariable(string Name, KickAssemblerParser.ForVarContext ParserContext): VariableDefinitionBase<KickAssemblerParser.ForVarContext>(Name, ParserContext);