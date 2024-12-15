using System.Collections.Frozen;
using Antlr4.Runtime;
using Righthand.RetroDbgDataProvider.Models;

namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

public class KickAssemblerParserListener: KickAssemblerParserBaseListener
{
   private readonly Dictionary<IToken, string> _fileReferences = new();
   private readonly HashSet<SegmentDefinitionInfo> _segmentDefinitions = new();
   public FrozenDictionary<IToken, string> FileReferences => _fileReferences.ToFrozenDictionary();
   public FrozenSet<SegmentDefinitionInfo> SegmentDefinitions => _segmentDefinitions.ToFrozenSet();
   public override void EnterPreprocessorImport(KickAssemblerParser.PreprocessorImportContext context)
   {
      var fileReference = context.fileReference;
      if (fileReference is not null)
      {
         _fileReferences.Add(fileReference, fileReference.Text);
      }
      base.EnterPreprocessorImport(context);
   }

   public override void ExitSegmentDef(KickAssemblerParser.SegmentDefContext context)
   {
      if (context.Name is not null)
      {
         var segmentInfo = new SegmentDefinitionInfo(context.Name.Text, context.Name.Line);
         _segmentDefinitions.Add(segmentInfo);
      }

      base.ExitSegmentDef(context);
   }
}