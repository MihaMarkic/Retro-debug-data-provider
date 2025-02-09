using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerParser;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerParserListener: KickAssemblerParserBaseListener
{
   private readonly Dictionary<IToken, string> _fileReferences = new();
   private readonly HashSet<SegmentDefinitionInfo> _segmentDefinitions = new();
   private readonly List<Label> _labelDefinitions = new();
   private readonly List<string> _variableDefinitions = new();
   private readonly List<Constant> _constantDefinitions = new();
   private readonly List<EnumValues> _enumValuesDefinitions = new();
   public FrozenDictionary<IToken, string> FileReferences => _fileReferences.ToFrozenDictionary();
    public FrozenSet<SegmentDefinitionInfo> SegmentDefinitions => [.. _segmentDefinitions];
   public ImmutableList<Label> LabelDefinitions => [.._labelDefinitions];
   public ImmutableList<string> VariableDefinitions => [.._variableDefinitions];
   public ImmutableList<Constant> ConstantDefinitions => [.._constantDefinitions];
   public ImmutableList<EnumValues> EnumValuesDefinitions => [.._enumValuesDefinitions];
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
         var segmentInfo = new SegmentDefinitionInfo(context.Name.Text, context.Name.Line);
         _segmentDefinitions.Add(segmentInfo);
      }

      base.ExitSegmentDef(context);
   }

   public override void EnterLabel(LabelContext context)
   {
      base.EnterLabel(context);
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
                _labelDefinitions.Add(label);
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
                    return new Label("", IsMultiOccurrence: true);
                }
                else if (context.ChildCount > 1 && context.GetChild(1) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING)
                {
                    return new Label(nameNode.GetText(), IsMultiOccurrence: true);
                }
                break;
            case AtNameContext atName when atName.ChildCount == 1 && atName.GetChild(0) is ITerminalNode nameNode && nameNode.Symbol.Type == UNQUOTED_STRING:
                return new Label(nameNode.GetText(), IsMultiOccurrence: false);
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
                _variableDefinitions.Add(data.Value.Left);
            }
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
                _constantDefinitions.Add(new Constant(data.Value.Left, data.Value.Right));
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
            _enumValuesDefinitions.Add(new EnumValues([.._tempEnumValues]));
        }
    }

    public override void EnterEnumValue(EnumValueContext context)
    {
        base.EnterEnumValue(context);
        
    }
    public override void ExitEnumValue([NotNull] EnumValueContext context)
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
}