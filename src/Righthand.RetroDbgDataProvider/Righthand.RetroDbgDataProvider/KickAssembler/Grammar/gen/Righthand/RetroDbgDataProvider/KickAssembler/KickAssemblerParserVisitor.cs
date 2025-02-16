//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from D:/Git/Righthand/C64/retro-dbg-data-provider/src/Righthand.RetroDbgDataProvider/Righthand.RetroDbgDataProvider/KickAssembler/Grammar/KickAssemblerParser.g4 by ANTLR 4.13.2

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Righthand.RetroDbgDataProvider.KickAssembler {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="KickAssemblerParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.2")]
[System.CLSCompliant(false)]
public interface IKickAssemblerParserVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.eol"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEol([NotNull] KickAssemblerParser.EolContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.program"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProgram([NotNull] KickAssemblerParser.ProgramContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.units"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnits([NotNull] KickAssemblerParser.UnitsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.unit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnit([NotNull] KickAssemblerParser.UnitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.errorSyntax"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitErrorSyntax([NotNull] KickAssemblerParser.ErrorSyntaxContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.label"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLabel([NotNull] KickAssemblerParser.LabelContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.instruction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInstruction([NotNull] KickAssemblerParser.InstructionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.scope"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitScope([NotNull] KickAssemblerParser.ScopeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.namedScope"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamedScope([NotNull] KickAssemblerParser.NamedScopeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.argumentList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArgumentList([NotNull] KickAssemblerParser.ArgumentListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.argument"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArgument([NotNull] KickAssemblerParser.ArgumentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.labelOffsetReference"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLabelOffsetReference([NotNull] KickAssemblerParser.LabelOffsetReferenceContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpression([NotNull] KickAssemblerParser.ExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.binaryop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBinaryop([NotNull] KickAssemblerParser.BinaryopContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.assignment_expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignment_expression([NotNull] KickAssemblerParser.Assignment_expressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.shorthand_assignment_expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShorthand_assignment_expression([NotNull] KickAssemblerParser.Shorthand_assignment_expressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.unary_operator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnary_operator([NotNull] KickAssemblerParser.Unary_operatorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.compareop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCompareop([NotNull] KickAssemblerParser.CompareopContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.classFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitClassFunction([NotNull] KickAssemblerParser.ClassFunctionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.function"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunction([NotNull] KickAssemblerParser.FunctionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.condition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCondition([NotNull] KickAssemblerParser.ConditionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.compiler_statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCompiler_statement([NotNull] KickAssemblerParser.Compiler_statementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.print"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPrint([NotNull] KickAssemblerParser.PrintContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.printnow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPrintnow([NotNull] KickAssemblerParser.PrintnowContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.forInit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForInit([NotNull] KickAssemblerParser.ForInitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.forVar"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForVar([NotNull] KickAssemblerParser.ForVarContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.var"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVar([NotNull] KickAssemblerParser.VarContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.const"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitConst([NotNull] KickAssemblerParser.ConstContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.if"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIf([NotNull] KickAssemblerParser.IfContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.errorif"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitErrorif([NotNull] KickAssemblerParser.ErrorifContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.eval"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEval([NotNull] KickAssemblerParser.EvalContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.evalAssignment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEvalAssignment([NotNull] KickAssemblerParser.EvalAssignmentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.break"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBreak([NotNull] KickAssemblerParser.BreakContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.watch"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWatch([NotNull] KickAssemblerParser.WatchContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.watchArguments"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWatchArguments([NotNull] KickAssemblerParser.WatchArgumentsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.enum"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnum([NotNull] KickAssemblerParser.EnumContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.enumValues"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumValues([NotNull] KickAssemblerParser.EnumValuesContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.enumValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumValue([NotNull] KickAssemblerParser.EnumValueContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.for"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFor([NotNull] KickAssemblerParser.ForContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.while"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWhile([NotNull] KickAssemblerParser.WhileContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.struct"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStruct([NotNull] KickAssemblerParser.StructContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.variableList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVariableList([NotNull] KickAssemblerParser.VariableListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.variable"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitVariable([NotNull] KickAssemblerParser.VariableContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.functionDefine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionDefine([NotNull] KickAssemblerParser.FunctionDefineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.return"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitReturn([NotNull] KickAssemblerParser.ReturnContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.macroDefine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMacroDefine([NotNull] KickAssemblerParser.MacroDefineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.pseudoCommandDefine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPseudoCommandDefine([NotNull] KickAssemblerParser.PseudoCommandDefineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.pseudoCommandDefineArguments"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPseudoCommandDefineArguments([NotNull] KickAssemblerParser.PseudoCommandDefineArgumentsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.namespace"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNamespace([NotNull] KickAssemblerParser.NamespaceContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.labelDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLabelDirective([NotNull] KickAssemblerParser.LabelDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.plugin"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPlugin([NotNull] KickAssemblerParser.PluginContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.segment"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSegment([NotNull] KickAssemblerParser.SegmentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.segmentDef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSegmentDef([NotNull] KickAssemblerParser.SegmentDefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.segmentOut"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSegmentOut([NotNull] KickAssemblerParser.SegmentOutContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fileDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFileDirective([NotNull] KickAssemblerParser.FileDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.diskDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDiskDirective([NotNull] KickAssemblerParser.DiskDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.diskDirectiveContent"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDiskDirectiveContent([NotNull] KickAssemblerParser.DiskDirectiveContentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.parameterMap"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterMap([NotNull] KickAssemblerParser.ParameterMapContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.parameterMapItems"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterMapItems([NotNull] KickAssemblerParser.ParameterMapItemsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.parameterMapItem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterMapItem([NotNull] KickAssemblerParser.ParameterMapItemContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.modify"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitModify([NotNull] KickAssemblerParser.ModifyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fileModify"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFileModify([NotNull] KickAssemblerParser.FileModifyContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.assert"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssert([NotNull] KickAssemblerParser.AssertContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.assertError"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssertError([NotNull] KickAssemblerParser.AssertErrorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.pseudopc"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPseudopc([NotNull] KickAssemblerParser.PseudopcContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.zp"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitZp([NotNull] KickAssemblerParser.ZpContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.zpArgumentList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitZpArgumentList([NotNull] KickAssemblerParser.ZpArgumentListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.zpArgument"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitZpArgument([NotNull] KickAssemblerParser.ZpArgumentContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fileName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFileName([NotNull] KickAssemblerParser.FileNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorDirective([NotNull] KickAssemblerParser.PreprocessorDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorDefine"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorDefine([NotNull] KickAssemblerParser.PreprocessorDefineContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorUndef"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorUndef([NotNull] KickAssemblerParser.PreprocessorUndefContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorImport"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorImport([NotNull] KickAssemblerParser.PreprocessorImportContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorImportIf"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorImportIf([NotNull] KickAssemblerParser.PreprocessorImportIfContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorImportOnce"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorImportOnce([NotNull] KickAssemblerParser.PreprocessorImportOnceContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorIf"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorIf([NotNull] KickAssemblerParser.PreprocessorIfContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorBlock([NotNull] KickAssemblerParser.PreprocessorBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.preprocessorCondition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPreprocessorCondition([NotNull] KickAssemblerParser.PreprocessorConditionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.directive"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDirective([NotNull] KickAssemblerParser.DirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.memoryDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMemoryDirective([NotNull] KickAssemblerParser.MemoryDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.cpuDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCpuDirective([NotNull] KickAssemblerParser.CpuDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.byteDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitByteDirective([NotNull] KickAssemblerParser.ByteDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.wordDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitWordDirective([NotNull] KickAssemblerParser.WordDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.dwordDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDwordDirective([NotNull] KickAssemblerParser.DwordDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.textDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTextDirective([NotNull] KickAssemblerParser.TextDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fillDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFillDirective([NotNull] KickAssemblerParser.FillDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fillDirectiveArguments"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFillDirectiveArguments([NotNull] KickAssemblerParser.FillDirectiveArgumentsContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fillExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFillExpression([NotNull] KickAssemblerParser.FillExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.encodingDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEncodingDirective([NotNull] KickAssemblerParser.EncodingDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.importDataDirective"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitImportDataDirective([NotNull] KickAssemblerParser.ImportDataDirectiveContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>MultiLabel</c>
	/// labeled alternative in <see cref="KickAssemblerParser.labelName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultiLabel([NotNull] KickAssemblerParser.MultiLabelContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>MultiAnonymousLabel</c>
	/// labeled alternative in <see cref="KickAssemblerParser.labelName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultiAnonymousLabel([NotNull] KickAssemblerParser.MultiAnonymousLabelContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>AtNameLabel</c>
	/// labeled alternative in <see cref="KickAssemblerParser.labelName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAtNameLabel([NotNull] KickAssemblerParser.AtNameLabelContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.atName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAtName([NotNull] KickAssemblerParser.AtNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.file"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFile([NotNull] KickAssemblerParser.FileContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.numberList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNumberList([NotNull] KickAssemblerParser.NumberListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.numericList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNumericList([NotNull] KickAssemblerParser.NumericListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.numeric"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNumeric([NotNull] KickAssemblerParser.NumericContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.number"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNumber([NotNull] KickAssemblerParser.NumberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.lohibyte"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLohibyte([NotNull] KickAssemblerParser.LohibyteContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.decNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDecNumber([NotNull] KickAssemblerParser.DecNumberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.hexNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitHexNumber([NotNull] KickAssemblerParser.HexNumberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.binNumber"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBinNumber([NotNull] KickAssemblerParser.BinNumberContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.boolean"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBoolean([NotNull] KickAssemblerParser.BooleanContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.opcodeExtension"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpcodeExtension([NotNull] KickAssemblerParser.OpcodeExtensionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.fullOpcode"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFullOpcode([NotNull] KickAssemblerParser.FullOpcodeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.opcode"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpcode([NotNull] KickAssemblerParser.OpcodeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.color"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitColor([NotNull] KickAssemblerParser.ColorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="KickAssemblerParser.opcodeConstant"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOpcodeConstant([NotNull] KickAssemblerParser.OpcodeConstantContext context);
}
} // namespace Righthand.RetroDbgDataProvider.KickAssembler
