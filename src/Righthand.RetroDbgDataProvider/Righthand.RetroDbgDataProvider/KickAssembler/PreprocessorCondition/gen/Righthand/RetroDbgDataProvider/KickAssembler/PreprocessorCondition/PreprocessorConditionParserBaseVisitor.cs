//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from D:/GitProjects/Righthand/C64/retro-dbg-data-provider/src/Righthand.RetroDbgDataProvider/Righthand.RetroDbgDataProvider/KickAssembler/PreprocessorCondition/PreprocessorConditionParser.g4 by ANTLR 4.13.2

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

/// <summary>
/// This class provides an empty implementation of <see cref="IPreprocessorConditionParserVisitor{Result}"/>,
/// which can be extended to create a visitor which only needs to handle a subset
/// of the available methods.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.2")]
[System.Diagnostics.DebuggerNonUserCode]
[System.CLSCompliant(false)]
public partial class PreprocessorConditionParserBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IPreprocessorConditionParserVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by the <c>ConditionBang</c>
	/// labeled alternative in <see cref="PreprocessorConditionParser.condition"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitConditionBang([NotNull] PreprocessorConditionParser.ConditionBangContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>ConditionParens</c>
	/// labeled alternative in <see cref="PreprocessorConditionParser.condition"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitConditionParens([NotNull] PreprocessorConditionParser.ConditionParensContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>ConditionOperation</c>
	/// labeled alternative in <see cref="PreprocessorConditionParser.condition"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitConditionOperation([NotNull] PreprocessorConditionParser.ConditionOperationContext context) { return VisitChildren(context); }
	/// <summary>
	/// Visit a parse tree produced by the <c>ConditionSymbol</c>
	/// labeled alternative in <see cref="PreprocessorConditionParser.condition"/>.
	/// <para>
	/// The default implementation returns the result of calling <see cref="AbstractParseTreeVisitor{Result}.VisitChildren(IRuleNode)"/>
	/// on <paramref name="context"/>.
	/// </para>
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	public virtual Result VisitConditionSymbol([NotNull] PreprocessorConditionParser.ConditionSymbolContext context) { return VisitChildren(context); }
}
} // namespace Righthand.RetroDbgDataProvider.KickAssembler.PreprocessorCondition
