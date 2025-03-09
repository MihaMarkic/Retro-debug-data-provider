using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Extensions;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Models;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerParser;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerParserListener: KickAssemblerParserBaseListener
{
   private readonly Dictionary<IToken, string> _fileReferences = new();
   private readonly ScopeBuilder _defaultScopeBuilder = new(context: null);
   private readonly Stack<ScopeBuilder> _scopes = new();
   private readonly List<KickAssemblerParserSyntaxError> _errors = new();
   public Scope? DefaultScope { get; private set; }
   /// <summary>
   /// Errors collected through invalid text in unit.
   /// </summary>
   /// <remarks>
   /// Parser is intentionally lax on this to allow better code completion.
   /// </remarks>
   public ImmutableArray<KickAssemblerParserSyntaxError> SyntaxErrors => [.._errors];
   public FrozenDictionary<IToken, string> FileReferences => _fileReferences.ToFrozenDictionary();
   private readonly Stack<VariablesScope> _variableScopes = new();
   private readonly Stack<ForScope> _forScopes = new();

   public override void EnterProgram(ProgramContext context)
   {
       _scopes.Push(_defaultScopeBuilder);
       base.EnterProgram(context);
   }

   public override void ExitProgram(ProgramContext context)
   {
       base.ExitProgram(context);
       DefaultScope = _defaultScopeBuilder.ToScope();
   }

   public override void ExitErrorSyntax(ErrorSyntaxContext context)
   {
       base.ExitErrorSyntax(context);
       _errors.Add(new KickAssemblerParserSyntaxError(context));
   }

   private void AddElementToCurrentScope(IScopeElement element)
   {
       _scopes.Peek().Elements.Add(element);
   }

   public override void EnterScope(ScopeContext context)
   {
       var newScope = new ScopeBuilder(context);
       _scopes.Push(newScope);
       base.EnterScope(context);
   }

   public override void ExitScope(ScopeContext context)
   {
       base.ExitScope(context);
       var scope = _scopes.Pop();
       var current = _scopes.Peek();
       current.Scopes.Add(scope);
   }

   public override void EnterPreprocessorImport(PreprocessorImportContext context)
   {
      var fileReference = context.fileReference;
      if (fileReference is not null)
      {
         _fileReferences.Add(fileReference, fileReference.Text);
      }
      base.EnterPreprocessorImport(context);
   }

   public override void ExitSegmentDef(SegmentDefContext context)
   {
      if (context.Name is not null)
      {
         var segmentInfo = new SegmentDefinitionInfo(context.Name.Text, context.Name.Line, context);
         AddElementToCurrentScope(segmentInfo);
      }

      base.ExitSegmentDef(context);
   }

    public override void ExitLabel([NotNull] LabelContext context)
    {
        base.ExitLabel(context);
        var labelName = context.labelName();
        if (labelName is not null)
        {
            var label = CreateLabel(labelName);
            if (label is not null)
            {
                AddElementToCurrentScope(label);
            }
        }
    }

    private static Label? CreateLabel(LabelNameContext context)
    {
        switch (context.GetChild(0))
        {
            case ITerminalNode terminalNode when terminalNode.Symbol.Type == BANG:
                if (context.ChildCount == 1)
                {
                    return new Label("", IsMultiOccurrence: true, context);
                }
                else if (context.ChildCount > 1 && context.GetChild(1) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING)
                {
                    return new Label(nameNode.GetText(), IsMultiOccurrence: true, context);
                }
                break;
            case AtNameContext { ChildCount: 1 } atName when atName.GetChild(0) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING:
                return new Label(nameNode.GetText(), IsMultiOccurrence: false, context);
        }

        return null;
    }

    public override void ExitVar(VarContext context)
    {
        base.ExitVar(context);
        var assignmentContext = context.assignment_expression();
        if (assignmentContext is not null)
        {
            var data = GetAssignment(assignmentContext);
            if (data is not null)
            {
                AddElementToCurrentScope(new Variable(data.Value.Left, context));
            }
        }
    }

    public override void ExitForVar(ForVarContext context)
    {
        base.ExitForVar(context);
        var assignmentContext = context.assignment_expression();
        if (assignmentContext is not null)
        {
            var data = GetAssignment(assignmentContext);
            if (data is not null)
            {
                var forScope = _forScopes.Peek();
                forScope.Variable = new (data.Value.Left, context);
            }
        }
    }

    public override void EnterFor(ForContext context)
    {
        _forScopes.Push(new());
        base.EnterFor(context);
    }

    public override void ExitFor(ForContext context)
    {
        base.ExitFor(context);
        var scope = _forScopes.Pop();
        if (scope.Variable is not null)
        {
            var forElement = new For([scope.Variable], context);
            AddElementToCurrentScope(forElement);
        }
    }

    public override void ExitConst(ConstContext context)
    {
        base.ExitConst(context);
        var assignmentContext = context.assignment_expression();
        if (assignmentContext is not null)
        {
            var data = GetAssignment(assignmentContext);
            if (data is not null)
            {
                AddElementToCurrentScope(new Constant(data.Value.Left, data.Value.Right, context));
            }
        }
    }

    private static (string Left, string Right)? GetAssignment(Assignment_expressionContext context)
    {
        if (context.ChildCount == 3 
            && context.GetChild(0) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING
            && context.GetChild(2) is ExpressionContext expressionContext)
        {
            return (nameNode.GetText(), expressionContext.GetText());
        }

        return null;
    }

    private readonly List<EnumValue> _tempEnumValues = new List<EnumValue>();
    public override void EnterEnumValues(EnumValuesContext context)
    {
        _tempEnumValues.Clear();
        base.EnterEnumValues(context);
    }

    public override void ExitEnumValues(EnumValuesContext context)
    {
        base.ExitEnumValues(context);
        if (_tempEnumValues.Count > 0)
        {
            AddElementToCurrentScope(new EnumValues([.._tempEnumValues], context));
        }
    }
    public override void ExitEnumValue(EnumValueContext context)
    {
        base.ExitEnumValue(context);
        if (context.GetChild(0) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING)
        {
            if (context.ChildCount == 1)
            {
                var enumValue = new EnumValue(nameNode.GetText());
                _tempEnumValues.Add(enumValue);
            }
            else if (context.ChildCount == 3 && context.GetChild(2) is NumberContext number)
            {
                var enumValue = new EnumValue(nameNode.GetText(), number.GetText());
                _tempEnumValues.Add(enumValue);
            }
        }
    }
    public override void EnterMacroDefine(MacroDefineContext context)
    {
        _variableScopes.Push();
        base.EnterMacroDefine(context);
    }

    public override void ExitMacroDefine(MacroDefineContext context)
    {
        base.ExitMacroDefine(context);
        var scope = _variableScopes.Pop();
        var atNameContext = context.atName();
        if (atNameContext is not null)
        {
            var atName = CreateAtName(atNameContext);
            if (atName is not null)
            {
                AddElementToCurrentScope(new Macro(atName.Value.Name, atName.Value.IsScopeEsc, [..scope.VariableNames], context));
            }
        }
    }

    public override void EnterFunctionDefine(FunctionDefineContext context)
    {
        _variableScopes.Push();
        base.EnterFunctionDefine(context);
    }

    public override void ExitFunctionDefine(FunctionDefineContext context)
    {
        base.ExitFunctionDefine(context);
        var scope = _variableScopes.Pop();
        var atNameContext = context.atName();
        if (atNameContext is not null)
        {
            var atName = CreateAtName(atNameContext);
            if (atName is not null)
            {
                AddElementToCurrentScope(new Function(atName.Value.Name, atName.Value.IsScopeEsc, [..scope.VariableNames], context));
            }
        }
    }

    public override void ExitVariable(VariableContext context)
    {
        base.ExitVariable(context);
        if (context.GetChild(0) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING)
        {
            var scope = _variableScopes.Peek();
            scope.VariableNames.Add(nameNode.GetText());
        }
    }

    private static AtName? CreateAtName(AtNameContext context)
    {
        return context.ChildCount switch
        {
            1 => new AtName(context.GetChild(0).GetText(), false),
            2 => new AtName(context.GetChild(1).GetText(), true),
            _ => null
        };
    }
    private readonly record struct AtName(string Name, bool IsScopeEsc);

    private class VariablesScope
    {
        public List<string> VariableNames { get; } = new ();
    }

    private class ForScope
    {
        public InitVariable? Variable { get; set; }
    }
}
