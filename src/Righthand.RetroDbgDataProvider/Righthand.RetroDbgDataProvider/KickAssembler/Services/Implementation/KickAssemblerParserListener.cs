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
   public FrozenDictionary<IToken, string> FileReferences => _fileReferences.ToFrozenDictionary();
    public FrozenSet<SegmentDefinitionInfo> SegmentDefinitions => [.. _segmentDefinitions];
   public ImmutableList<Label> LabelDefinitions => [.._labelDefinitions];
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

    internal static Label? CreateLabel(LabelNameContext context)
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
}